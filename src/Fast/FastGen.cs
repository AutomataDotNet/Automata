using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Z3;

using Microsoft.Automata.Z3;
using Microsoft.Automata;

using Microsoft.Automata.Z3.Internal;

namespace Microsoft.Fast
{
    /// <summary>
    /// Sample Fast generator
    /// </summary>
    public class FastGen
    {
        String name;
        public Func<int, string> GetStateName;
        public Func<int, string> GetVarName;
        public int initialState;

        TreeTheory tt;
        public FastGen(Z3Provider z)
        {
            this.initialState = 0;
            this.tt = z.TT;
            this.GetStateName = s =>
            {
                if ((int)s == initialState)
                    return name;
                else
                    return name + "_q" + s;
            };
            this.GetVarName = (v => "x" + v);
        }

        /// <summary>
        /// Write the tree automaton definition to sb.
        /// If isAcceptor is true, the output will be a language acceptor definition.
        /// </summary>
        public void ToFastTree(String name, RankedAlphabetSort alph, IEnumerable<Expr> trees, StringBuilder sb)
        {
            sb.AppendLine();            
            foreach (var res in trees)
            {
                if (res != null)
                {
                    sb.Append(string.Format("Tree {0} : {1} := ", name, alph.alphSort.Name.ToString()));
                    ToFastExpr(res, sb, false);
                    sb.AppendLine();
                }
                else
                    sb.AppendLine(string.Format("// the input is accepted by {0}", name));
                return;
            }
            sb.AppendLine(string.Format("// the input is NOT accepted by {0}", name));                    
        }

        /// <summary>
        /// Write the tree automaton definition to sb.
        /// If isAcceptor is true, the output will be a language acceptor definition.
        /// </summary>
        public void ToFast(String name, TreeTransducer ta, StringBuilder sb, bool isAcc)
        {
            this.name = name;
            this.initialState = int.Parse(ta.Root.ToString());
            //ToFastTrans(ta, sb);
            if (!ta.IsEmpty)
                //if (ta.IsPureAcceptorState(ta.InitialState))
                //    ToFastLang(ta, sb);
                //else
                    ToFastTrans(ta, sb);
            else
            {
                #region Empty transducers or languages
                if (!isAcc)
                {
                    sb.Append(string.Format("Public Trans {0} : {1} -> {2}", GetStateName(int.Parse(ta.Root.ToString())),
                        ta.InputAlphabet.AlphabetSort.Name.ToString(), ta.OutputAlphabet.AlphabetSort.Name.ToString()));
                    sb.AppendLine("{ }");
                }
                else
                {
                    sb.Append(string.Format("Public Lang {0} : {1}", GetStateName(int.Parse(ta.Root.ToString())),
                        ta.InputAlphabet.AlphabetSort.Name.ToString()));
                    sb.AppendLine("{ }");
                }
                sb.AppendLine(string.Format("// WARNING: The transducer {0} is empty",name));
                Console.WriteLine("The transducer {0} is empty", name);
                #endregion
            }
        }

        void ToFastTrans(TreeTransducer ta, StringBuilder sb)
        {
            foreach (var state in ta.GetStates())
            {
                //Check if state is a trans or a lang
                if (ta.IsPureAcceptorState(state) || ta.IsPureTransducerState(state))               
                {
                    if (ta.IsPureAcceptorState(state))
                    {
                        sb.AppendFormat((state.Equals(ta.Root) ? "Public " : "") +
                        "Lang {0} : {1} ", GetStateName(int.Parse(state.ToString())),
                        ta.InputAlphabet.AlphabetSort.Name.ToString());
                    }
                    else
                    {
                        sb.AppendFormat((state.Equals(ta.Root) ? "Public " : "") +
                        "Trans {0} : {1} -> {2} ", GetStateName(int.Parse(state.ToString())),
                        ta.InputAlphabet.AlphabetSort.Name.ToString(), ta.OutputAlphabet.AlphabetSort.Name.ToString());
                    }
                    sb.AppendLine("{");
                    bool needCaseSeparator = false;
                    foreach (var func in ta.InputAlphabet.Symbols)
                    {
                        var rules = ta.GetRules(state, func);
                        for (int i = 0; i < rules.Count; i++)
                        {
                            sb.Append("\t");
                            if (needCaseSeparator)
                                sb.Append("| ");  //case separator
                            else
                                sb.Append("  ");

                            var rule = rules[i];
                            sb.Append(rule.Symbol.Name.ToString());
                            sb.Append("(");
                            for (int j = 1; j <= rule.Rank; j++)
                            {
                                if (j > 1)
                                    sb.Append(",");
                                sb.Append("x" + j);
                            }
                            sb.Append(") where ");
                            ToFastExpr(rule.Guard, sb, true);

                            var output_given = true;
                            for (int j = 1; j <= rule.Rank; j++)
                            {
                                var args_j = rule.Lookahead(j-1);
                                foreach (var rlastate in args_j)
                                {
                                    if ((!rlastate.Equals(tt.identityState)) && ta.IsPureAcceptorState(rlastate))
                                    {
                                        if (output_given)
                                        {
                                            sb.Append(" given ");
                                            output_given = false;
                                        }
                                        sb.Append("(");
                                        sb.Append(GetStateName(tt.Z.GetNumeralInt(rlastate)));
                                        sb.Append(" x" + j);
                                        sb.Append(")");
                                    }
                                }
                            }

                            if (ta.IsPureTransducerState(state))
                            {
                                sb.Append(" to ");
                                ToFastExpr(rule.Output, sb, false);
                            }
                            sb.AppendLine();
                            needCaseSeparator = true;
                        }
                    }
                    sb.AppendLine("}");
                }
                else
                    throw new Exception("Method To fast trans");
            }
        }

        /// <summary>
        /// Write the alphabet definition into sb.
        /// E.g. A[s:string,b:bool,i:int]{zero(0),one(1),two(2)}
        /// </summary>
        public void ToFast(RankedAlphabet ra, StringBuilder sb)
        {
            sb.Append("Alphabet ");
            sb.Append(ra.AlphabetSort.Name.ToString());

            FuncDecl[] attrFields;
            try
            {
                attrFields = ((TupleSort)ra.AttrSort).FieldDecls;
            }
            catch
            {
                throw new AutomataException(AutomataExceptionKind.Fast_AttributeMustBeTuple);
            }

            sb.Append("[");
            for (int i = 0; i < attrFields.Length; i++)
            {
                if (i > 0)
                    sb.Append(",");
                sb.Append(attrFields[i].Name.ToString());
                sb.Append(":");
                sb.Append(GetSortName(tt.Z.GetRange(attrFields[i])));
            }
            sb.Append("]{");
            for (int i = 0; i < ra.constructors.Length; i++)
            {
                if (i > 0)
                    sb.Append(",");
                sb.Append(ra.constructors[i].Name.ToString());
                sb.Append("(");
                sb.Append(ra.ranks[i]);
                sb.Append(")");
            }
            sb.AppendLine("}");
        }

        //Normalizes sort name from Z3 to fast
        private string GetSortName(Sort sort)
        {
            if (sort.Equals(tt.Z.StringSort))
                return "string";
            else
            {
                var str = sort.Name.ToString();
                if (str == "Int")
                    return "int";
                else if (str == "Real")
                    return "real";
                else if (str == "Bool")
                    return "bool";
                return str;
            }
        }

        /// <summary>
        /// Write the enum definition into sb.
        /// E.g. enum color {R,B,G}
        /// </summary>
        public void ToFast(string enum_name, StringBuilder sb)
        {
            sb.Append("Enum ");
            sb.Append(enum_name);
            sb.Append("{");
            bool addComma = false;
            foreach (var e in tt.Z.GetEnumElements(enum_name))
            {
                if (addComma)
                    sb.Append(",");
                sb.Append(e);
                addComma = true;
            }
            sb.AppendLine("}");
        }

        //Prints a Z3 expression in sb, the flag isAttr is activated if t is an attribute
        internal void ToFastExpr(Expr t, StringBuilder sb, bool isAttr)
        {
            Sort s = t.Sort;
            var tk = t.ASTKind;
            if (tt.Z.IsVar(t))
            {
                sb.Append(GetVarName((int)tt.Z.GetVarIndex(t)));
                return;
            }
            switch (tk)
            {
                case Z3_ast_kind.Z3_NUMERAL_AST:
                    {
                        #region integer or real
                        var t_str = t.ToString();
                        if (s is RealSort)
                        {
                            if (!t_str.Contains(".") && !t_str.Contains("/"))
                                t_str = t_str + ".0";
                            if (t_str.StartsWith("-"))
                                t_str = String.Format("(- 0.0 {0}) ", t_str.Substring(1));
                        }
                        else
                        {
                            if (t_str.StartsWith("-"))
                                t_str = String.Format("(- 0 {0}) ", t_str.Substring(1));
                        }
                        sb.Append(t_str);
                        break;
                        #endregion
                    }
                //case Z3_ast_kind.Z3_VAR_AST:
                //    sb.Append(GetVarName((int)tt.Z.GetVarIndex(t)));
                //    break;
                case Z3_ast_kind.Z3_APP_AST:
                    {
                        #region true or false
                        if (t.Equals(tt.Z.True))
                        {
                            sb.Append("true");
                            break;
                        }

                        if (t.Equals(tt.Z.False))
                        {
                            sb.Append("false");
                            break;
                        }
                        #endregion

                        var f = t.FuncDecl;
                        var args = t.Args;
                        var dk = f.DeclKind;
                        switch (dk)
                        {
                            case Z3_decl_kind.Z3_OP_UNINTERPRETED:
                                {
                                    #region transductions, constructors, and field accessors
                                    if (f.Name.ToString().StartsWith("$trans"))
                                    {
                                        if (args[0].ToString() == "-1")
                                        {
                                            //identity mapping
                                            sb.Append(GetVarName((int)tt.Z.GetVarIndex(args[1])));
                                        }
                                        else
                                        {
                                            sb.Append("(");
                                            sb.Append(GetStateName(int.Parse(args[0].ToString())));
                                            sb.Append(" ");
                                            sb.Append(GetVarName((int)tt.Z.GetVarIndex(args[1])));
                                            sb.Append(")");
                                        }
                                    }
                                    else if (args.Length == 1 && tt.Z.IsVar(args[0]) && tt.Z.GetVarIndex(args[0]) == 0)
                                    {
                                        //if f is a field name then just use that field name
                                        if (tt.Z.IsTupleField(args[0].Sort, f))
                                            sb.Append(f.Name.ToString());
                                        else
                                        {
                                            sb.Append("(");
                                            sb.Append(f.Name.ToString());
                                            sb.Append(" ");
                                            ToFastAttribute(args[0], sb);
                                            sb.Append(")");
                                        }
                                    }
                                    else if (s is DatatypeSort)
                                    {
                                        sb.Append("(");
                                        sb.Append(f.Name.ToString());
                                        sb.Append(" ");
                                        if (isAttr)
                                            ToFastExpr(args[0], sb, isAttr);
                                        else
                                            ToFastAttribute(args[0], sb);
                                        for (int i = 1; i < args.Length; i++)
                                        {
                                            sb.Append(" ");
                                            ToFastExpr(args[i], sb, isAttr);
                                        }
                                        sb.Append(")");
                                    }
                                    else if (tt.Z.GetDomain(f).Length == 1 && tt.Z.GetDomain(f)[0].Name.ToString().StartsWith("$") && tt.Z.IsTupleField(tt.Z.GetDomain(f)[0], f) &&
                                        args[0].ASTKind == Z3_ast_kind.Z3_APP_AST && args[0].FuncDecl.Name.ToString().StartsWith("$"))
                                    {
                                        int f_nr = tt.Z.GetTupleFieldNr(tt.Z.GetDomain(f)[0], f);
                                        Expr t1 = args[0].Args[f_nr];
                                        ToFastExpr(t1, sb, isAttr);
                                    }                                                                        
                                    else
                                    {
                                        throw new AutomataException(AutomataExceptionKind.UnexpectedDeclKind);
                                    }
                                    #endregion
                                    break;
                                }
                            case Z3_decl_kind.Z3_OP_DT_CONSTRUCTOR:{                                 
                                if (args.Length == 0)
                                {
                                    //either empty string or enum
                                    if (t.Equals(tt.Z.GetNil(tt.Z.StringSort)))
                                        sb.Append("\"\"");
                                    else
                                    {
                                        //t must be an enum value
                                        sb.Append(s.Name.ToString());
                                        sb.Append(".");
                                        sb.Append(f.Name.ToString());
                                    }
                                }else if (s.Equals(tt.Z.StringSort))
                                {
                                    // f is a string constructed as a list (non-empty)                                    
                                    sb.Append(tt.Z.PrettyPrintCS(t, v => { throw new AutomataException(AutomataExceptionKind.TreeTheory_UnexpectedVariable); }));
                                }                                
                                else{
                                    // f is a tree constructor, 
                                    // first arg is the attribute
                                    // other args are the subtrees
                                    sb.Append("(");
                                    sb.Append(f.Name.ToString());
                                    sb.Append(" ");
                                    if (isAttr)
                                        ToFastExpr(args[0], sb, isAttr);
                                    else
                                        ToFastAttribute(args[0], sb);
                                    for (int i = 1; i < args.Length; i++)
                                    {
                                        sb.Append(" ");
                                        ToFastExpr(args[i], sb, isAttr);
                                    }
                                    sb.Append(")");
                                }                                
                                break;
                            }
                            case Z3_decl_kind.Z3_OP_DT_ACCESSOR:
                                {
                                    // f is a tuple accessor 
                                    // z3 tuple accessers are of the form (i x0) to access element i  
                                    // only outputs func name 'i' and not the tuple x0
                                    sb.Append(f.Name.ToString());
                                    break;
                                }
                            default: //just use the builtin name of the function symbol, should revisit the names in fast
                                {
                                    var fname = f.Name.ToString().ToLower();

                                    if (tt.Z.GetDomain(f).Length == 1 && tt.Z.GetDomain(f)[0].Name.ToString().StartsWith("$") && tt.Z.IsTupleField(tt.Z.GetDomain(f)[0], f) &&
                                        args[0].ASTKind == Z3_ast_kind.Z3_APP_AST && args[0].FuncDecl.Name.ToString().StartsWith("$"))
                                    {
                                        int f_nr = tt.Z.GetTupleFieldNr(tt.Z.GetDomain(f)[0], f);
                                        Expr t1 = args[0].Args[f_nr];
                                        ToFastExpr(t1, sb, isAttr);
                                    }

                                    #region simplify trivial conjunctions with true
                                    if (fname == "and") //simplify a common case
                                    {
                                        args = Array.FindAll(args, arg => !tt.Z.True.Equals(arg));
                                        if (args.Length == 0)
                                        {
                                            sb.Append("true");
                                            break;
                                        }
                                        else if (args.Length == 1)
                                        {
                                            ToFastExpr(args[0], sb, isAttr);
                                            break;
                                        }
                                    }
                                    #endregion

                                    if (fname == "=")
                                        fname = "==";

                                    sb.Append("(");
                                    sb.Append(fname); //ignore case
                                    foreach (var arg in args)
                                    {
                                        sb.Append(" ");
                                        ToFastExpr(arg, sb, isAttr);
                                    }
                                    sb.Append(")");
                                    break;
                                }
                        }
                    }
                    break;
                default:
                    throw new AutomataException(AutomataExceptionKind.UnexpectedExprKind);
            }
        }

        private void ToFastAttribute(Expr term, StringBuilder sb)
        {

            //here assuming that the attribute is a tuple and that f is tuple constructor (possibly empty tuple)
            if (tt.Z.IsVar(term) && tt.Z.GetVarIndex(term) == 0)
            {
                sb.Append("[");
                var tuple = term.Sort;
                var length = tt.Z.GetTupleLength(tuple);
                for (int i = 0; i < length; i++)
                {
                    if (i > 0)
                        sb.Append(",");
                    sb.Append(tt.Z.GetTupleField(tuple, i).Name.ToString());
                }
                sb.Append("]");
                return;
            }
            else
            {
                if (!(term.FuncDecl.Name.ToString().StartsWith("$")))
                {
                    throw new AutomataException(AutomataExceptionKind.Fast_AttributeMustBeTuple);
                }

                sb.Append("[");

                var args = term.Args;
                for (int i = 0; i < args.Length; i++)
                {
                    if (i > 0)
                        sb.Append(",");
                    ToFastExpr(args[i], sb, true);
                }
                sb.Append("]");
            }

        }

        void ToFastLang(TreeTransducer ta, StringBuilder sb)
        {
            foreach (var state in ta.GetStates())
            {
                sb.AppendLine(string.Format((state.Equals(ta.Root) ? "Public" : "") + " Lang {0} : {1}", GetStateName(int.Parse(state.ToString())), ta.InputAlphabet.AlphabetSort.Name.ToString()));
                sb.AppendLine("{");
                bool needCaseSeparator = false;
                foreach (var func in ta.InputAlphabet.Symbols)
                {
                    var rules = ta.GetRules(state, func);
                    for (int i = 0; i < rules.Count; i++)
                    {
                        sb.Append("\t");
                        if (needCaseSeparator)
                            sb.Append("| ");  //case separator
                        else
                            sb.Append(" ");

                        var rule = rules[i];
                        sb.Append(rule.Symbol.Name.ToString());
                        sb.Append("(");
                        for (int j = 1; j <= rule.Rank; j++)
                        {
                            if (j > 1)
                                sb.Append(",");
                            sb.Append("x" + j);
                        }
                        sb.Append(") where ");
                        ToFastExpr(rule.Guard, sb, true);
                        bool output_given = true;
                        for (int j = 1; j <= rule.Rank; j++)
                        {
                            var given_j = rule.Lookahead(j-1);
                            foreach (var given_c in given_j)
                            {
                                if (!given_c.Equals(tt.identityState))
                                {
                                    if (output_given)
                                    {
                                        sb.Append(" given ");
                                        output_given = false;
                                    }
                                    sb.Append("(");
                                    sb.Append(GetStateName(tt.Z.GetNumeralInt(given_c)));
                                    sb.Append(" x" + j);
                                    sb.Append(")");
                                }
                            }
                        }
                        needCaseSeparator = true;
                        sb.AppendLine();
                    }
                }
                sb.AppendLine("}");
            }
        }
    }
}
