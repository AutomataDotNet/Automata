using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Z3;

namespace Microsoft.Automata.Z3.Internal
{
    /// <summary>
    /// Pretty printer of Z3 terms.
    /// </summary>
    internal class ExprPrettyPrinter
    {
        internal bool treat_numerals_as_bv16 = true; 
        Z3Provider z3p;
        string falseName = "false";
        string trueName = "true";
        Sort charSort;

        /// <summary>
        /// Creates a pretty printer of Z3 terms in the given z3 provider z3p.
        /// Lists of concrete characters of the given numeric sort z3p.CharSort
        /// are displayed as escaped strings encolsed in doublequotes. 
        /// Individual constants of sort z3p.CharSort are displayed as 
        /// escaped characters enclosed in single-quotes.
        /// </summary>
        /// <param name="z3p">given z3 provider</param>
        public ExprPrettyPrinter(Z3Provider z3p)
        {
            if (z3p == null)
                throw new ArgumentNullException("z3p");

            this.charSort = z3p.CharSort;
            this.z3p = z3p;
        }

        bool IsInfixOperator(Z3_decl_kind declKind)
        {
            return
                (declKind == Z3_decl_kind.Z3_OP_OR ||
                 declKind == Z3_decl_kind.Z3_OP_AND ||
                 declKind == Z3_decl_kind.Z3_OP_UGT ||
                 declKind == Z3_decl_kind.Z3_OP_UGEQ ||
                 declKind == Z3_decl_kind.Z3_OP_ULEQ ||
                 declKind == Z3_decl_kind.Z3_OP_ULT ||
                 declKind == Z3_decl_kind.Z3_OP_EQ ||
                 declKind == Z3_decl_kind.Z3_OP_ADD ||
                 declKind == Z3_decl_kind.Z3_OP_SUB ||
                 declKind == Z3_decl_kind.Z3_OP_MUL ||
                 declKind == Z3_decl_kind.Z3_OP_BADD ||
                 declKind == Z3_decl_kind.Z3_OP_BSUB ||
                 declKind == Z3_decl_kind.Z3_OP_BMUL ||
                 declKind == Z3_decl_kind.Z3_OP_BAND ||
                 declKind == Z3_decl_kind.Z3_OP_BOR ||
                 declKind == Z3_decl_kind.Z3_OP_BUDIV ||
                 declKind == Z3_decl_kind.Z3_OP_BUDIV0 ||
                 declKind == Z3_decl_kind.Z3_OP_BUDIV_I ||
                 declKind == Z3_decl_kind.Z3_OP_BSDIV ||
                 declKind == Z3_decl_kind.Z3_OP_BSDIV0 ||
                 declKind == Z3_decl_kind.Z3_OP_BSDIV_I ||
                 declKind == Z3_decl_kind.Z3_OP_BUREM ||
                 declKind == Z3_decl_kind.Z3_OP_BUREM0 ||
                 declKind == Z3_decl_kind.Z3_OP_BUREM_I ||
                 declKind == Z3_decl_kind.Z3_OP_BSREM ||
                 declKind == Z3_decl_kind.Z3_OP_BSREM0 ||
                 declKind == Z3_decl_kind.Z3_OP_BSREM_I ||
                 declKind == Z3_decl_kind.Z3_OP_DIV || 
                 declKind == Z3_decl_kind.Z3_OP_REM ||
                 declKind == Z3_decl_kind.Z3_OP_BLSHR ||
                 declKind == Z3_decl_kind.Z3_OP_BSHL);
        }

        bool IsDisjOrConj(Z3_decl_kind declKind)
        {
            return (declKind == Z3_decl_kind.Z3_OP_OR || declKind == Z3_decl_kind.Z3_OP_AND);
        }

        bool IsBitDisjOrConj(Z3_decl_kind declKind)
        {
            return (declKind == Z3_decl_kind.Z3_OP_BOR || declKind == Z3_decl_kind.Z3_OP_BAND);
        }

        string DescribeInfixOperator(Z3_decl_kind declKind)
        {
            switch (declKind)
            {
                case Z3_decl_kind.Z3_OP_OR:
                    if (compactview)
                        return "|";
                    else
                        return "||";
                case Z3_decl_kind.Z3_OP_AND:
                    if (compactview)
                        return "&";
                    else
                        return "&&";
                case Z3_decl_kind.Z3_OP_GT:
                case Z3_decl_kind.Z3_OP_UGT:
                    return ">";
                case Z3_decl_kind.Z3_OP_LT:
                case Z3_decl_kind.Z3_OP_ULT:
                    return "<";
                case Z3_decl_kind.Z3_OP_LE:
                case Z3_decl_kind.Z3_OP_ULEQ:
                    return "<=";
                case Z3_decl_kind.Z3_OP_GE:
                case Z3_decl_kind.Z3_OP_UGEQ:
                    return ">=";
                case Z3_decl_kind.Z3_OP_EQ:
                    if (compactview)
                        return "=";
                    else
                        return "==";
                case Z3_decl_kind.Z3_OP_ADD:
                    return "+";
                case Z3_decl_kind.Z3_OP_SUB:
                    return "-";
                case Z3_decl_kind.Z3_OP_MUL:
                    return "*";
                case Z3_decl_kind.Z3_OP_BADD:
                    return "+";
                case Z3_decl_kind.Z3_OP_BSUB:
                    return "-";
                case Z3_decl_kind.Z3_OP_BMUL:
                    return "*";
                case Z3_decl_kind.Z3_OP_BAND:
                    return "&";
                case Z3_decl_kind.Z3_OP_BOR:
                    return "|";
                case Z3_decl_kind.Z3_OP_BSDIV0:
                case Z3_decl_kind.Z3_OP_BSDIV:
                case Z3_decl_kind.Z3_OP_BSDIV_I:
                case Z3_decl_kind.Z3_OP_BUDIV0:
                case Z3_decl_kind.Z3_OP_BUDIV:
                case Z3_decl_kind.Z3_OP_BUDIV_I:
                    return "/";
                case Z3_decl_kind.Z3_OP_BSREM0:
                case Z3_decl_kind.Z3_OP_BSREM:
                case Z3_decl_kind.Z3_OP_BSREM_I:
                case Z3_decl_kind.Z3_OP_BUREM0:
                case Z3_decl_kind.Z3_OP_BUREM:
                case Z3_decl_kind.Z3_OP_BUREM_I:
                    return "%";
                case Z3_decl_kind.Z3_OP_DIV:
                    return "/";
                case Z3_decl_kind.Z3_OP_REM:
                    return "%";
                case Z3_decl_kind.Z3_OP_BLSHR:
                    return ">>";
                case Z3_decl_kind.Z3_OP_BSHL:
                    return "<<";
                default:
                    throw new AutomataException(AutomataExceptionKind.UnspecifiedOperatorDeclarationKind);
            }
        }

        internal bool compactview = false;
        /// <summary>
        /// Returns a pretty printed view of the given term.
        /// </summary>
        /// <param name="term">given term</param>
        /// <returns>pretty printed view of the given term</returns>
        internal string DescribeExpr(Expr term)
        {
            string res = DescribeExpr1(term);
            return res;
        }
        internal string DescribeExpr1(Expr term)
        {

            if (z3p.IsVar(term))
                return LookupVarName(term);

            var kind = term.ASTKind;
            if (kind == Z3_ast_kind.Z3_NUMERAL_AST)
            {
                int code = (int)z3p.GetNumeralUInt(term); //???TBD???
                if (CSmode)
                {
                    return DisplayNumber(code);
                }

                if (charSort == null || !charSort.Equals(z3p.GetSort(term)) || code < 32 || code > 126)
                {
                    return DisplayNumber(code);
                }
                else
                {
                    char c = (char)code;
                    return string.Format("'{0}'", Escaper.Escape(c));
                }
            }
            else if (kind == Z3_ast_kind.Z3_APP_AST)
            {
                #region function application
                FuncDecl decl = z3p.GetAppDecl(term);
                var dkind = decl.DeclKind;
                Expr[] subterms = z3p.GetAppArgs(term);
                if (compactview)
                {
                    if (dkind == Z3_decl_kind.Z3_OP_DT_CONSTRUCTOR)
                    {
                        #region compact viewing of lists
                        if (decl.Name.ToString().Equals("cons") && subterms.Length == 2)
                        {
                            string first = DescribeExpr(subterms[0]);
                            string rest = DescribeExpr(subterms[1]);
                            if (rest.StartsWith("[") && rest.EndsWith("]"))
                            {
                                if (rest == "[]")
                                {
                                    return "[" + first + "]";
                                }
                                else
                                {
                                    var list = "[" + first + "," +
                                        rest.Substring(1, rest.Length - 2) + "]";
                                    return list;
                                }
                            }
                        }
                        else if (decl.Name.ToString().Equals("nil") && subterms.Length == 0)
                        {
                            return "[]";
                        }
                        #endregion
                    }
                    else if (dkind == Z3_decl_kind.Z3_OP_DT_ACCESSOR)
                    {
                        #region view head(tail(tail(c)) as c2
                        if (decl.Name.ToString().Equals("head") && subterms.Length == 1)
                        {
                            var rest_term = subterms[0];
                            FuncDecl rest_decl = z3p.GetAppDecl(rest_term);
                            var rest_dkind = rest_decl.DeclKind;
                            int rest_count = 0;
                            while (rest_dkind == Z3_decl_kind.Z3_OP_DT_ACCESSOR && rest_decl.Name.ToString().Equals("tail"))
                            {
                                rest_term = z3p.GetAppArgs(rest_term)[0];
                                rest_decl = z3p.GetAppDecl(rest_term);
                                rest_dkind = rest_decl.DeclKind;
                                rest_count += 1;
                            }
                            var counter_name = DescribeExpr(rest_term);
                            if (z3p.IsVar(rest_term))
                            {
                                counter_name += rest_count;
                                return counter_name;
                            }
                        }
                        #endregion
                    }
                }
                Expr[] terms;
                if (dkind == Z3_decl_kind.Z3_OP_NOT)
                {
                    #region negation
                    //there must be a single argument
                    if (subterms[0].Equals(z3p.True))
                        return falseName;
                    else if (subterms[0].Equals(z3p.False))
                        return trueName;
                    else
                        return "!" + DescribeExpr(subterms[0]) + "";
                    #endregion
                }
                else if(dkind == Z3_decl_kind.Z3_OP_DT_CONSTRUCTOR){
                    string str;
                    // Input is a list of bitvectors: output corresponding string
                    if (IsCocreteCharacterList(term, out str))
                        return str;
                    string res = decl.Name + "(";
                    if (subterms.Length > 0)
                        res += DescribeExpr(subterms[0]);
                    for (int i = 1; i < subterms.Length; i++)
                    {
                        res += ",";
                        res += DescribeExpr(subterms[i]);
                    }
                    res += ")";
                    return res;
                }
                else if (dkind == Z3_decl_kind.Z3_OP_EXTRACT)
                {                        //there must be exactly one argument
                    if (subterms.Length != 1)
                        throw new AutomataException(AutomataExceptionKind.UnExpectedNrOfOperands);

                    var declparams = decl.Parameters;
                    int k = declparams[0].Int;
                    int m = declparams[1].Int;

                    //see first if subterms[0] is fixed numeric
                    string s1 = DescribeExpr(subterms[0]);

                    int _nr = 0;
                    if (TryGetNumber(s1, out _nr))
                    {
                        int mask = 0;
                        for (int i = m; i <= k; i++)
                        {
                            mask = mask | (1 << i);
                        }
                        int _res = (_nr & mask) >> m;
                        return DisplayNumber(_res);
                    }

                    int mask_k_m = (1 << (k - m + 1)) - 1;
                    string res2;
                    if (m == 0) //no need to shift left
                        res2 = string.Format("({0}&{1})", s1, string.Format("0x{0:X}", mask_k_m));
                    else //shift left m bits
                        res2 = string.Format("(({0}>>{1})&{2})", s1, m, string.Format("0x{0:X}", mask_k_m));
                    return res2;
                }
                else if (IsConcat(term, out terms))
                {
                    string result = DescribeExpr(terms[0]);

                    for (int i = 1; i < terms.Length; ++i)
                    {
                        string s2 = DescribeExpr(terms[i]);
                        uint sz = ((BitVecSort)(terms[i].Sort)).Size;
                        result = ShiftLeftAndBitOr(result, s2, sz);
                    }
                    return result;
                }
                else if (IsInfixOperator(dkind))
                {
                    if (IsDisjOrConj(dkind))
                    {
                        #region conjunction or disjunction may take any nr of arguments
                        string[] descr = Array.ConvertAll(subterms, DescribeExpr);
                        if (descr.Length == 0)
                            return (dkind == Z3_decl_kind.Z3_OP_AND ? trueName : falseName);
                        else if (descr.Length == 1)
                            return descr[0];
                        else
                        {
                            List<string> simplified = new List<string>();
                            foreach (var x in descr)
                                if (x == (dkind == Z3_decl_kind.Z3_OP_AND ? falseName : trueName))
                                    return (dkind == Z3_decl_kind.Z3_OP_AND ? falseName : trueName);
                                else if (x != (dkind == Z3_decl_kind.Z3_OP_AND ? trueName : falseName))
                                    simplified.Add(x);

                            if (simplified.Count == 0)
                                return (dkind == Z3_decl_kind.Z3_OP_AND ? trueName : falseName);

                            if (simplified.Count == 1)
                                return simplified[0];

                            string s = "(" + simplified[0];
                            for (int i = 1; i < simplified.Count; i++)
                                s += (DescribeInfixOperator(dkind) + simplified[i]);
                            s += ")";

                            return s;
                        }
                        #endregion
                    }
                    //else if (dkind == Z3_decl_kind.Z3_OP_BShl || dkind == Z3_decl_kind.Z3_OP_BLShr)
                    //{
                    //    #region special binary infix operators with fixed second argument
                    //    //there must be exactly one argument
                    //    if (subterms.Length != 1)
                    //        throw new AutomataException(AutomataExceptionKind.UnExpectedNrOfOperands);

                    //    IParameter[] declparams = z3p.z3.GetDeclParameters(decl);
                    //    int k = int.Parse(declparams[0].ToString());

                    //    string s1 = DescribeExpr(subterms[0]);
                    //    return string.Format("({0}{1}{2})", s1, DescribeInfixOperator(dkind), k);
                    //    #endregion
                    //}
                    else if (IsBitDisjOrConj(dkind))
                    {
                        if (subterms.Length < 2)
                            throw new AutomataException(AutomataExceptionKind.UnExpectedNrOfOperands);

                        string res = "(" + DescribeExpr(subterms[0]);
                        for (int i = 1; i < subterms.Length; i++)
                            res = res + DescribeInfixOperator(dkind) + DescribeExpr(subterms[i]);
                        res += ")";
                        return res;

                    }
                    else
                    {
                        #region binary infix operators
                        //there must be exactly two arguments
                        if (subterms.Length < 2)
                            throw new AutomataException(AutomataExceptionKind.UnExpectedNrOfOperands);


                        string[] subs = Array.ConvertAll(subterms, DescribeExpr);
                        string res = subs[0];

                        for (int i = 1; i < subs.Length; i++)
                            res = string.Format("({0}{1}{2})", res, DescribeInfixOperator(dkind), subs[i]);

                        if (subterms[0].Sort is BitVecSort && (dkind == Z3_decl_kind.Z3_OP_BADD || dkind == Z3_decl_kind.Z3_OP_BMUL))
                        {
                            uint sz = (subterms[0].Sort as BitVecSort).Size;
                            if (sz < (uint)z3p.Encoding)
                            {
                                int mask = ~(-1 << (int)sz);
                                string maskStr = Escaper.EscapeHex(mask);
                                res = string.Format("({0}{1}{2})", res, DescribeInfixOperator(Z3_decl_kind.Z3_OP_BAND), maskStr);
                            }
                        }
                        return res;
                        #endregion
                    }
                }
                else if (dkind == Z3_decl_kind.Z3_OP_FALSE)
                    return falseName; //false
                else if (dkind == Z3_decl_kind.Z3_OP_TRUE)
                    return trueName; //true
                else if (dkind == Z3_decl_kind.Z3_OP_ITE)
                {
                    #region if-then-else term
                    string cond = DescribeExpr(subterms[0]);
                    string trueCase = DescribeExpr(subterms[1]);
                    string falseCase = DescribeExpr(subterms[2]);
                    if (cond == trueName)
                        return trueCase;
                    else if (cond == falseCase)
                        return falseCase;
                    else
                    {
                        if (!CSmode)
                            return string.Format("ite({0},{1},{2})", EliminateOuterParanthesis(cond), EliminateOuterParanthesis(trueCase), EliminateOuterParanthesis(falseCase));
                        else
                            return string.Format("({0} ? {1} : {2})", EliminateOuterParanthesis(cond), EliminateOuterParanthesis(trueCase), EliminateOuterParanthesis(falseCase));
                    }
                    #endregion
                }
                else if (dkind == Z3_decl_kind.Z3_OP_UNINTERPRETED)
                {
                    #region function symbols, such as declared constants, Skolem functions, tuples, Lists, etc.
                    string f = z3p.GetDeclShortName(decl);
                    string f_fullname = z3p.GetDeclName(decl);
                    uint[] ps = z3p.GetDeclParameters(decl);
                    int arity = subterms.Length;

                    if (f.Equals("T0")) //the default empty tuple
                        return "T()";

                    if (f.Equals("hex")) //compute the hexadacimal digit encoder
                    {
                        if (ps.Length > 1)
                            throw new AutomataException(AutomataExceptionKind.InternalError);
                        uint k = (ps.Length == 0 ? 0 : ps[0]);
                        string c = (k == 0 ? DescribeExpr(subterms[0]) : string.Format("({0}>>{1})", DescribeExpr(subterms[0]), k * 4));
                        string res = (CSmode ? string.Format("(({0}&0xF)+(({0}&0xF)<=9 ? 48 : 55))", c) :
                                               string.Format("(({0}&0xF)+ite(({0}&0xF)<=9, 48, 55))", c));
                        return res;
                    }

                    string tupleConstructorName = Z3Provider.tupleSortNamePrefix + arity.ToString();

                    bool isTupleConstr = false;
                    if (f.Equals(tupleConstructorName))
                    {
                        isTupleConstr = true;
                        f = ""; //ignore the tuple constructor
                    }
                    if (f.StartsWith("."))
                        //tuple projection: use postfix notation
                        return string.Format("{0}{1}", DescribeExpr(subterms[0]), f);

                    string str = null;
                    if (IsCocreteCharacterList(term, out str))
                        return str;


                    if (f == "bvurem_i")
                    {
                        return string.Format("({0}%{1})", DescribeExpr(subterms[0]), DescribeExpr(subterms[1]));
                    }

                    if (f == "bvudiv_i")
                    {
                        return string.Format("({0}/{1})", DescribeExpr(subterms[0]), DescribeExpr(subterms[1]));
                    }

                    if (subterms.Length == 0)
                        return f;
                    else
                    {
                        string s0 = (isTupleConstr ? "T(" : f + "(");
                        string s1 = (isTupleConstr ? ")" : ")");
                        for (int j = 0; j < ps.Length; j++)
                        {
                            s0 += (j > 0 ? "," : "");
                            s0 += ps[j];
                        }
                        if (ps.Length > 0 && subterms.Length > 0)
                            s0 += ",";
                        string s = s0 + EliminateOuterParanthesis(DescribeExpr(subterms[0]));
                        for (int i = 1; i < subterms.Length; i++)
                            s += ("," + EliminateOuterParanthesis(DescribeExpr(subterms[i])));
                        return s + s1;
                    }
                    #endregion
                }
                else if (dkind == Z3_decl_kind.Z3_OP_DT_ACCESSOR)
                {
                    return string.Format("{0}.{1}", DescribeExpr(subterms[0]), z3p.GetDeclShortName(decl));
                }
                else
                    return term.ToString();
                //throw new ArgumentException(string.Format("unexpected function declaration kind {0}", dkind), "term");
                #endregion
            }
            else
            {
                throw new AutomataException(AutomataExceptionKind.UnexpectedExprKindInPrettyPrinter);
            }
        }

        private string ShiftLeftAndBitOr(string result, string s2, uint sz)
        {
            int _res;
            if (TryGetNumber(result, out _res))
            {
                int _s2;
                int m = _res << ((int)sz);
                if (TryGetNumber(s2, out _s2))
                {
                    int k = m | _s2;
                    string k_str = DisplayNumber(k);
                    return k_str;
                }
                string str = (m == 0 ? s2 : string.Format("({0}|{1})", DisplayNumber(m), s2));
                return str;
            }
            if (s2 == "0")
                return string.Format("({0}<<{1})", result, sz, s2);
            else
                return string.Format("(({0}<<{1})|{2})", result, sz, s2);
        }

        private bool TryGetNumber(string s1, out int _nr)
        {
            if (s1.StartsWith("0x"))
            {
                _nr = int.Parse(s1.Substring(2), System.Globalization.NumberStyles.HexNumber);
                return true;
            }
            else if (int.TryParse(s1, out _nr))
            {
                return true;
            }
            _nr = 0;
            return false;
        }

        private bool IsConcat(Expr term, out Expr[] terms)
        {
            terms = null;
            if (term.ASTKind != Z3_ast_kind.Z3_APP_AST) return false;
            if (term.FuncDecl.DeclKind != Z3_decl_kind.Z3_OP_CONCAT) return false;
            terms = term.Args;
            return true;
        }

        private string DisplayNumber(int code)
        {
            if (code >= 0 && code <= 9)
                return code.ToString();
            if (treat_numerals_as_bv16)
                return string.Format("0x{0:X}", ((short)code));
            else
                return code.ToString();
        }


        internal Func<Expr, string> __lookupVarName = null;
        string LookupVarName(Expr var)
        {
            if (__lookupVarName == null)
                return var.ToString();
            else
                return __lookupVarName(var);
        }

        bool CSmode = false;
        /// <summary>
        /// Returns a pretty printed view of the given term.
        /// </summary>
        /// <param name="term">the given term</param>
        /// <param name="lookupVarName">lookup function for variable names</param>
        public string DescribeExpr(Expr term, Func<Expr, string> lookupVarName)
        {
            __lookupVarName = lookupVarName;
            var str = DescribeExpr(term);
            __lookupVarName = null;
            return str;
        }

        /// <summary>
        /// Returns a pretty printed view of the given term. Uses C# compliant expression notation.
        /// </summary>
        /// <param name="term">the given term</param>
        /// <param name="lookupVarName">lookup function for variable names</param>
        public string DescribeExprCS(Expr term, Func<Expr, string> lookupVarName)
        {
            CSmode = true;
            __lookupVarName = lookupVarName;
            var str = DescribeExpr(term);
            __lookupVarName = null;
            CSmode = false;
            return str;
        }

        string EliminateOuterParanthesis(string cond)
        {
            if (cond.StartsWith("("))
                //must also end with ")"
                return cond.Substring(1, cond.Length - 2);
            else
                return cond;
        }

        bool IsCocreteCharacterList(Expr term, out string str)
        {
            if (charSort == null)
            {
                str = null;
                return false;
            }

            Sort StringSort = z3p.GetSort(term);

            if (!(StringSort.Name.ToString().Equals("List") &&
                  z3p.GetElemSort(StringSort).Equals(charSort)))
            {
                str = null;
                return false;
            }

            Expr EmptyString = z3p.GetNil(StringSort);

            if (term.Equals(EmptyString))
            {
                str = "nil";
                return true;
            }

            string res = "\"";
            var curr = term;
            while (z3p.GetAppDecl(curr).Equals(z3p.GetCons(StringSort)))
            {
                var first = z3p.GetAppArgs(curr)[0];
                var elemKind = first.ASTKind;
                var rest = z3p.GetAppArgs(curr)[1];
                if (!elemKind.Equals(Z3_ast_kind.Z3_NUMERAL_AST) ||
                    !(rest.Equals(EmptyString) ||
                      (rest.ASTKind == Z3_ast_kind.Z3_APP_AST &&
                       z3p.GetAppDecl(rest).Equals(z3p.GetCons(StringSort)))))
                {
                    str = null;
                    return false;
                }
                else
                {
                    char c = (char)z3p.GetNumeralUInt(first);
                    res += Rex.RexEngine.Escape(c);
                }
                curr = z3p.GetAppArgs(curr)[1];
            }
            if (curr.Equals(z3p.GetNil(StringSort)))
            {
                str = res + "\"";
                return true;
            }
            else
            {
                str = null;
                return false;
            }
        }
    }

    /// <summary>
    /// Pretty printer of Z3 terms.
    /// </summary>
    internal class CSPrettyPrinter
    {
        internal bool treat_numerals_as_bv16 = true;
        Expr tt;
        Expr ff;
        string falseName = "false";
        string trueName = "true";
        Sort charSort;
        uint encoding;

        /// <summary>
        /// Z3 expr to C# pretty printer
        /// </summary>
        public CSPrettyPrinter(Context z3)
        {
            this.charSort = z3.MkBitVecSort(16);
            this.tt = z3.MkBool(true);
            this.ff = z3.MkBool(false);
            this.encoding = 16;
        }

        bool IsInfixOperator(Z3_decl_kind declKind)
        {
            return
                (declKind == Z3_decl_kind.Z3_OP_OR ||
                 declKind == Z3_decl_kind.Z3_OP_AND ||
                 declKind == Z3_decl_kind.Z3_OP_UGT ||
                 declKind == Z3_decl_kind.Z3_OP_UGEQ ||
                 declKind == Z3_decl_kind.Z3_OP_ULEQ ||
                 declKind == Z3_decl_kind.Z3_OP_ULT ||
                 declKind == Z3_decl_kind.Z3_OP_EQ ||
                 declKind == Z3_decl_kind.Z3_OP_ADD ||
                 declKind == Z3_decl_kind.Z3_OP_SUB ||
                 declKind == Z3_decl_kind.Z3_OP_MUL ||
                 declKind == Z3_decl_kind.Z3_OP_BADD ||
                 declKind == Z3_decl_kind.Z3_OP_BSUB ||
                 declKind == Z3_decl_kind.Z3_OP_BMUL ||
                 declKind == Z3_decl_kind.Z3_OP_BAND ||
                 declKind == Z3_decl_kind.Z3_OP_BOR ||
                 declKind == Z3_decl_kind.Z3_OP_BUDIV ||
                 declKind == Z3_decl_kind.Z3_OP_BUREM ||
                 declKind == Z3_decl_kind.Z3_OP_DIV ||
                 declKind == Z3_decl_kind.Z3_OP_REM ||
                 declKind == Z3_decl_kind.Z3_OP_BLSHR ||
                 declKind == Z3_decl_kind.Z3_OP_BSHL);
        }

        bool IsDisjOrConj(Z3_decl_kind declKind)
        {
            return (declKind == Z3_decl_kind.Z3_OP_OR || declKind == Z3_decl_kind.Z3_OP_AND);
        }

        bool IsBitDisjOrConj(Z3_decl_kind declKind)
        {
            return (declKind == Z3_decl_kind.Z3_OP_BOR || declKind == Z3_decl_kind.Z3_OP_BAND);
        }

        string DescribeInfixOperator(Z3_decl_kind declKind)
        {
            switch (declKind)
            {
                case Z3_decl_kind.Z3_OP_OR:
                    return "||";
                case Z3_decl_kind.Z3_OP_AND:
                    return "&&";
                case Z3_decl_kind.Z3_OP_GT:
                case Z3_decl_kind.Z3_OP_UGT:
                    return ">";
                case Z3_decl_kind.Z3_OP_LT:
                case Z3_decl_kind.Z3_OP_ULT:
                    return "<";
                case Z3_decl_kind.Z3_OP_LE:
                case Z3_decl_kind.Z3_OP_ULEQ:
                    return "<=";
                case Z3_decl_kind.Z3_OP_GE:
                case Z3_decl_kind.Z3_OP_UGEQ:
                    return ">=";
                case Z3_decl_kind.Z3_OP_EQ:
                    return "==";
                case Z3_decl_kind.Z3_OP_ADD:
                    return "+";
                case Z3_decl_kind.Z3_OP_SUB:
                    return "-";
                case Z3_decl_kind.Z3_OP_MUL:
                    return "*";
                case Z3_decl_kind.Z3_OP_BADD:
                    return "+";
                case Z3_decl_kind.Z3_OP_BSUB:
                    return "-";
                case Z3_decl_kind.Z3_OP_BMUL:
                    return "*";
                case Z3_decl_kind.Z3_OP_BAND:
                    return "&";
                case Z3_decl_kind.Z3_OP_BOR:
                    return "|";
                case Z3_decl_kind.Z3_OP_BUDIV:
                    return "/";
                case Z3_decl_kind.Z3_OP_BUREM:
                    return "%";
                case Z3_decl_kind.Z3_OP_DIV:
                    return "/";
                case Z3_decl_kind.Z3_OP_REM:
                    return "%";
                case Z3_decl_kind.Z3_OP_BLSHR:
                    return ">>";
                case Z3_decl_kind.Z3_OP_BSHL:
                    return "<<";
                default:
                    throw new AutomataException(AutomataExceptionKind.UnspecifiedOperatorDeclarationKind);
            }
        }

        /// <summary>
        /// Returns a pretty printed view of the given term.
        /// </summary>
        /// <param name="term">given term</param>
        /// <returns>pretty printed view of the given term</returns>
        internal string DescribeExpr(Expr term)
        {
            string res = DescribeExpr1(term);
            return res;
        }


        public uint GetNumeralUInt(Expr t)
        {
            var ts = t.Sort;
            if (t is IntNum)
                return ((IntNum)t).UInt;
            if (t is BitVecNum)
                return ((BitVecNum)t).UInt;
            //avoiding z3 bug: (GetSort(t) is TupleSort) == false even when t is a tuple
            if ((t.Sort.Name.ToString().StartsWith("$")) && (GetTupleLength(t.Sort) == 1))
            {
                var v = t.Args[0];
                if (v is IntNum)
                    return ((IntNum)v).UInt;
                if (v is BitVecNum)
                    return ((BitVecNum)v).UInt;
                throw new Exception();
            }
            throw new Exception();
        }

        public int GetTupleLength(Sort tupleSort)
        {
            if (tupleSort is DatatypeSort)
                return (int)((DatatypeSort)tupleSort).Accessors[0].Length;
            if (tupleSort is TupleSort)
                return (int)((TupleSort)tupleSort).FieldDecls.Length;
            throw new Exception();
        }

        bool IsVar(Expr x)
        {
            if (x.IsConst && x.FuncDecl.Name.IsIntSymbol())
                return true;
            else
                return false;
        }

        internal string DescribeExpr1(Expr term)
        {
            if (IsVar(term))
                return LookupVarName(term);

            var kind = term.ASTKind;
            if (kind == Z3_ast_kind.Z3_NUMERAL_AST)
            {
                int code = (int)GetNumeralUInt(term);
                //if (CSmode)
                {
                    return DisplayNumber(code);
                }

                //if (charSort == null || !charSort.Equals(term.Sort) || code < 32 || code > 126)
                //{
                //    return DisplayNumber(code);
                //}
                //else
                //{
                //    char c = (char)code;
                //    return string.Format("'{0}'", Escaper.Escape(c));
                //}
            }
            //else if (kind == Z3_ast_kind.Z3_VAR_AST)
            //{
            //    #region input or output variable
            //    ////can only be either input or output variable
            //    //if (term.Equals(MkInputVar(GetSort(term))))
            //    //    return inputVarName;
            //    //else if (term.Equals(MkOutputVar(GetSort(term))))
            //    //    return outputVarName;
            //    //else
            //    return LookupVarName(term);
            //    #endregion
            //}
            else if (kind == Z3_ast_kind.Z3_APP_AST)
            {
                #region function application
                FuncDecl decl = term.FuncDecl;
                var dkind = decl.DeclKind;
                Expr[] subterms = term.Args;
                Expr[] terms;
                if (dkind == Z3_decl_kind.Z3_OP_NOT)
                {
                    #region negation
                    //there must be a single argument
                    if (subterms[0].Equals(tt))
                        return falseName;
                    else if (subterms[0].Equals(ff))
                        return trueName;
                    else
                        return "!" + DescribeExpr(subterms[0]) + "";
                    #endregion
                }
                else if (dkind == Z3_decl_kind.Z3_OP_DT_CONSTRUCTOR)
                {
                    // Input is a list of bitvectors: output corresponding string
                    //string str;
                    //if (IsCocreteCharacterList(term, out str))
                    //    return str;
                    return term.ToString();
                    //TODO
                    //throw new AutomataException("Unexpected constructor");
                }
                else if (dkind == Z3_decl_kind.Z3_OP_EXTRACT)
                {                        //there must be exactly one argument
                    if (subterms.Length != 1)
                        throw new AutomataException(AutomataExceptionKind.UnExpectedNrOfOperands);

                    var declparams = decl.Parameters;
                    int k = declparams[0].Int;
                    int m = declparams[1].Int;

                    //see first if subterms[0] is fixed numeric
                    string s1 = DescribeExpr(subterms[0]);

                    int _nr = 0;
                    if (TryGetNumber(s1, out _nr))
                    {
                        int mask = 0;
                        for (int i = m; i <= k; i++)
                        {
                            mask = mask | (1 << i);
                        }
                        int _res = (_nr & mask) >> m;
                        return DisplayNumber(_res);
                    }

                    int mask_k_m = (1 << (k - m + 1)) - 1;
                    string res2;
                    if (m == 0) //no need to shift left
                        res2 = string.Format("({0}&{1})", s1, string.Format("0x{0:X}", mask_k_m));
                    else //shift left m bits
                        res2 = string.Format("(({0}>>{1})&{2})", s1, m, string.Format("0x{0:X}", mask_k_m));
                    return res2;
                }
                else if (IsConcat(term, out terms))
                {
                    string result = DescribeExpr(terms[0]);

                    for (int i = 1; i < terms.Length; ++i)
                    {
                        string s2 = DescribeExpr(terms[i]);
                        uint sz = ((BitVecSort)(terms[i].Sort)).Size;
                        result = ShiftLeftAndBitOr(result, s2, sz);
                    }
                    return result;
                }
                else if (IsInfixOperator(dkind))
                {
                    if (IsDisjOrConj(dkind))
                    {
                        #region conjunction or disjunction may take any nr of arguments
                        string[] descr = Array.ConvertAll(subterms, DescribeExpr);
                        if (descr.Length == 0)
                            return (dkind == Z3_decl_kind.Z3_OP_AND ? trueName : falseName);
                        else if (descr.Length == 1)
                            return descr[0];
                        else
                        {
                            List<string> simplified = new List<string>();
                            foreach (var x in descr)
                                if (x == (dkind == Z3_decl_kind.Z3_OP_AND ? falseName : trueName))
                                    return (dkind == Z3_decl_kind.Z3_OP_AND ? falseName : trueName);
                                else if (x != (dkind == Z3_decl_kind.Z3_OP_AND ? trueName : falseName))
                                    simplified.Add(x);

                            if (simplified.Count == 0)
                                return (dkind == Z3_decl_kind.Z3_OP_AND ? trueName : falseName);

                            if (simplified.Count == 1)
                                return simplified[0];

                            string s = "(" + simplified[0];
                            for (int i = 1; i < simplified.Count; i++)
                                s += (DescribeInfixOperator(dkind) + simplified[i]);
                            s += ")";

                            return s;
                        }
                        #endregion
                    }
                    //else if (dkind == Z3_decl_kind.Z3_OP_BShl || dkind == Z3_decl_kind.Z3_OP_BLShr)
                    //{
                    //    #region special binary infix operators with fixed second argument
                    //    //there must be exactly one argument
                    //    if (subterms.Length != 1)
                    //        throw new AutomataException(AutomataExceptionKind.UnExpectedNrOfOperands);

                    //    IParameter[] declparams = z3p.z3.GetDeclParameters(decl);
                    //    int k = int.Parse(declparams[0].ToString());

                    //    string s1 = DescribeExpr(subterms[0]);
                    //    return string.Format("({0}{1}{2})", s1, DescribeInfixOperator(dkind), k);
                    //    #endregion
                    //}
                    else if (IsBitDisjOrConj(dkind))
                    {
                        if (subterms.Length < 2)
                            throw new AutomataException(AutomataExceptionKind.UnExpectedNrOfOperands);

                        string res = "(" + DescribeExpr(subterms[0]);
                        for (int i = 1; i < subterms.Length; i++)
                            res = res + DescribeInfixOperator(dkind) + DescribeExpr(subterms[i]);
                        res += ")";
                        return res;

                    }
                    else
                    {
                        #region binary infix operators
                        //there must be exactly two arguments
                        if (subterms.Length < 2)
                            throw new AutomataException(AutomataExceptionKind.UnExpectedNrOfOperands);


                        string[] subs = Array.ConvertAll(subterms, DescribeExpr);
                        string res = subs[0];

                        for (int i = 1; i < subs.Length; i++)
                            res = string.Format("({0}{1}{2})", res, DescribeInfixOperator(dkind), subs[i]);

                        if (subterms[0].Sort is BitVecSort && (dkind == Z3_decl_kind.Z3_OP_BADD || dkind == Z3_decl_kind.Z3_OP_BMUL))
                        {
                            uint sz = (subterms[0].Sort as BitVecSort).Size;
                            if (sz < encoding)
                            {
                                int mask = ~(-1 << (int)sz);
                                string maskStr = Escaper.EscapeHex(mask);
                                res = string.Format("({0}{1}{2})", res, DescribeInfixOperator(Z3_decl_kind.Z3_OP_BAND), maskStr);
                            }
                        }
                        return res;
                        #endregion
                    }
                }
                else if (dkind == Z3_decl_kind.Z3_OP_FALSE)
                    return falseName; //false
                else if (dkind == Z3_decl_kind.Z3_OP_TRUE)
                    return trueName; //true
                else if (dkind == Z3_decl_kind.Z3_OP_ITE)
                {
                    #region if-then-else term
                    string cond = DescribeExpr(subterms[0]);
                    string trueCase = DescribeExpr(subterms[1]);
                    string falseCase = DescribeExpr(subterms[2]);
                    if (cond == trueName)
                        return trueCase;
                    else if (cond == falseCase)
                        return falseCase;
                    else
                    {
                        //if (!CSmode)
                        //    return string.Format("ite({0},{1},{2})", EliminateOuterParanthesis(cond), EliminateOuterParanthesis(trueCase), EliminateOuterParanthesis(falseCase));
                        //else
                            return string.Format("({0} ? {1} : {2})", EliminateOuterParanthesis(cond), EliminateOuterParanthesis(trueCase), EliminateOuterParanthesis(falseCase));
                    }
                    #endregion
                }
                else if (dkind == Z3_decl_kind.Z3_OP_UNINTERPRETED)
                {
                    #region function symbols, such as declared constants, Skolem functions, tuples, Lists, etc.
                    string f_fullname = decl.Name.ToString();
                    string f = System.Text.RegularExpressions.Regex.Split(decl.Name.ToString(), @"\[.*\]$")[0];
                    uint[] ps =  Array.ConvertAll(decl.Parameters, p => (uint)p.Int);
                    int arity = subterms.Length;

                    if (f.Equals("T0")) //the default empty tuple
                        return "T()";

                    if (f.Equals("hex")) //compute the hexadacimal digit encoder
                    {
                        if (ps.Length > 1)
                            throw new AutomataException(AutomataExceptionKind.InternalError);
                        uint k = (ps.Length == 0 ? 0 : ps[0]);
                        string c = (k == 0 ? DescribeExpr(subterms[0]) : string.Format("({0}>>{1})", DescribeExpr(subterms[0]), k * 4));
                        string res = string.Format("(({0}&0xF)+(({0}&0xF)<=9 ? 48 : 55))",c);
                        return res;
                    }

                    string tupleConstructorName = Z3Provider.tupleSortNamePrefix + arity.ToString();

                    bool isTupleConstr = false;
                    if (f.Equals(tupleConstructorName))
                    {
                        isTupleConstr = true;
                        f = ""; //ignore the tuple constructor
                    }
                    if (f.StartsWith("."))
                        //tuple projection: use postfix notation
                        return string.Format("{0}{1}", DescribeExpr(subterms[0]), f);

                    //string str = null;
                    //if (IsCocreteCharacterList(term, out str))
                    //    return str;


                    if (f == "bvurem_i")
                    {
                        return string.Format("({0}%{1})", DescribeExpr(subterms[0]), DescribeExpr(subterms[1]));
                    }

                    if (f == "bvudiv_i")
                    {
                        return string.Format("({0}/{1})", DescribeExpr(subterms[0]), DescribeExpr(subterms[1]));
                    }

                    if (subterms.Length == 0)
                        return f;
                    else
                    {
                        string s0 = (isTupleConstr ? "T(" : f + "(");
                        string s1 = (isTupleConstr ? ")" : ")");
                        for (int j = 0; j < ps.Length; j++)
                        {
                            s0 += (j > 0 ? "," : "");
                            s0 += ps[j];
                        }
                        if (ps.Length > 0 && subterms.Length > 0)
                            s0 += ",";
                        string s = s0 + EliminateOuterParanthesis(DescribeExpr(subterms[0]));
                        for (int i = 1; i < subterms.Length; i++)
                            s += ("," + EliminateOuterParanthesis(DescribeExpr(subterms[i])));
                        return s + s1;
                    }
                    #endregion
                }
                else
                    return term.ToString();
                //throw new ArgumentException(string.Format("unexpected function declaration kind {0}", dkind), "term");
                #endregion
            }
            else
            {
                throw new AutomataException(AutomataExceptionKind.UnexpectedExprKindInPrettyPrinter);
            }
        }

        private string ShiftLeftAndBitOr(string result, string s2, uint sz)
        {
            int _res;
            if (TryGetNumber(result, out _res))
            {
                int _s2;
                int m = _res << ((int)sz);
                if (TryGetNumber(s2, out _s2))
                {
                    int k = m | _s2;
                    string k_str = DisplayNumber(k);
                    return k_str;
                }
                string str = (m == 0 ? s2 : string.Format("({0}|{1})", DisplayNumber(m), s2));
                return str;
            }
            if (s2 == "0")
                return string.Format("({0}<<{1})", result, sz, s2);
            else
                return string.Format("(({0}<<{1})|{2})", result, sz, s2);
        }

        private bool TryGetNumber(string s1, out int _nr)
        {
            if (s1.StartsWith("0x"))
            {
                _nr = int.Parse(s1.Substring(2), System.Globalization.NumberStyles.HexNumber);
                return true;
            }
            else if (int.TryParse(s1, out _nr))
            {
                return true;
            }
            _nr = 0;
            return false;
        }

        private bool IsConcat(Expr term, out Expr[] terms)
        {
            terms = null;
            if (term.ASTKind != Z3_ast_kind.Z3_APP_AST) return false;
            if (term.FuncDecl.DeclKind != Z3_decl_kind.Z3_OP_CONCAT) return false;
            terms = term.Args;
            return true;
        }

        private string DisplayNumber(int code)
        {
            if (code >= 0 && code <= 9)
                return code.ToString();
            if (treat_numerals_as_bv16)
                return string.Format("0x{0:X}", ((short)code));
            else
                return code.ToString();
        }


        Func<Expr, string> __lookupVarName = null;
        string LookupVarName(Expr var)
        {
            if (__lookupVarName == null)
                return var.ToString();
            else
                return __lookupVarName(var);
        }

        //bool CSmode = false;
        /// <summary>
        /// Returns a pretty printed view of the given term.
        /// </summary>
        /// <param name="term">the given term</param>
        /// <param name="lookupVarName">lookup function for variable names</param>
        public string DescribeExpr(Expr term, Func<Expr, string> lookupVarName)
        {
            __lookupVarName = lookupVarName;
            var str = DescribeExpr(term);
            __lookupVarName = null;
            return str;
        }

        /// <summary>
        /// Returns a pretty printed view of the given term. Uses C# compliant expression notation.
        /// </summary>
        /// <param name="term">the given term</param>
        /// <param name="lookupVarName">lookup function for variable names</param>
        public string DescribeExprCS(Expr term, Func<Expr, string> lookupVarName)
        {
            //CSmode = true;
            __lookupVarName = lookupVarName;
            var str = DescribeExpr(term);
            __lookupVarName = null;
            //CSmode = false;
            return str;
        }

        string EliminateOuterParanthesis(string cond)
        {
            if (cond.StartsWith("("))
                //must also end with ")"
                return cond.Substring(1, cond.Length - 2);
            else
                return cond;
        }

        //bool IsCocreteCharacterList(Expr term, out string str)
        //{
        //    if (charSort == null)
        //    {
        //        str = null;
        //        return false;
        //    }

        //    Sort StringSort = term.Sort;

        //    if (!(StringSort.Name.ToString().Equals("List") &&
        //          z3p.GetElemSort(StringSort).Equals(charSort)))
        //    {
        //        str = null;
        //        return false;
        //    }

        //    Expr EmptyString = z3p.GetNil(StringSort);

        //    if (term.Equals(EmptyString))
        //    {
        //        str = "nil";
        //        return true;
        //    }

        //    string res = "\"";
        //    var curr = term;
        //    while (z3p.GetAppDecl(curr).Equals(z3p.GetCons(StringSort)))
        //    {
        //        var first = z3p.GetAppArgs(curr)[0];
        //        var elemKind = first.ASTKind;
        //        var rest = z3p.GetAppArgs(curr)[1];
        //        if (!elemKind.Equals(Z3_ast_kind.Z3_NUMERAL_AST) ||
        //            !(rest.Equals(EmptyString) ||
        //              (rest.ASTKind == Z3_ast_kind.Z3_APP_AST &&
        //               z3p.GetAppDecl(rest).Equals(z3p.GetCons(StringSort)))))
        //        {
        //            str = null;
        //            return false;
        //        }
        //        else
        //        {
        //            char c = (char)z3p.GetNumeralUInt(first);
        //            res += Rex.RexEngine.Escape(c);
        //        }
        //        curr = z3p.GetAppArgs(curr)[1];
        //    }
        //    if (curr.Equals(z3p.GetNil(StringSort)))
        //    {
        //        str = res + "\"";
        //        return true;
        //    }
        //    else
        //    {
        //        str = null;
        //        return false;
        //    }
        //}
    }
}
