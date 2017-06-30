using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Z3;

using Microsoft.Automata;

using Microsoft.Automata.Z3.Internal;

using ExprSet = Microsoft.Automata.OrderedSet<Microsoft.Z3.Expr>;

namespace Microsoft.Automata.Z3
{
    /// <summary>
    /// Encapsulates functionality used for reasoning about ranked trees.
    /// </summary>
    public class TreeTheory
    {
        public Z3Provider Z;

        public Expr identityState;
        public const int IdentityStateId = -1;

        internal TreeTheory(Z3Provider z)
        {
            this.Z = z;
            this.identityState = z.MkInt(IdentityStateId);
        }

        Dictionary<Sort, RankedAlphabet> rankedAlphabetInfoMap = new Dictionary<Sort, RankedAlphabet>();

        Dictionary<Tuple<string, Sort>, Sort> rankedAlphabetSortMap = new Dictionary<Tuple<string, Sort>, Sort>();

        /// <summary>
        /// Returns true iff the given sort is a ranked alphabet sort.
        /// </summary>
        /// <param name="sort">given sort</param>
        bool IsRankedAlphabetSort(Sort sort)
        {
            return rankedAlphabetInfoMap.ContainsKey(sort);
        }

        /// <summary>
        /// Gets the ranked alphabet sort with the given name and node sort.
        /// </summary>
        /// <param name="name">alphabet name</param>
        /// <param name="nodeSort">sort of node labels</param>
        public Sort GetRankedAlphabetSort(string name, Sort nodeSort)
        {
            Sort sort;
            if (!rankedAlphabetSortMap.TryGetValue(new Tuple<string, Sort>(name, nodeSort), out sort))
                throw new AutomataException(AutomataExceptionKind.RankedAlphabet_UnrecognizedAlphabetSort);
            else
                return sort;
        }

        /// <summary>
        /// Gets the ranked alphabet of the given sort.
        /// </summary>
        /// <param name="alphabetSort">ranked alphabet sort</param>
        public RankedAlphabet GetRankedAlphabet(Sort alphabetSort)
        {
            RankedAlphabet alph;
            if (!rankedAlphabetInfoMap.TryGetValue(alphabetSort, out alph))
                throw new AutomataException(AutomataExceptionKind.RankedAlphabet_UnrecognizedAlphabetSort);
            else
                return alph;
        }

        /// <summary>
        /// Creates a ranked alphabet, a rank of a function symbol is the number of its children (subtrees).
        /// The rank excludes the node label. Thus, all function symbols have rank + 1 number of arguments.
        /// The first argument is always the attribute.
        /// </summary>
        /// <param name="name">alphabet name</param>
        /// <param name="attributeSort">attribute sort</param>
        /// <param name="symbols">function symbols of the alphabet</param>
        /// <param name="ranks">ranks of the function symbols</param>
        public RankedAlphabet MkRankedAlphabet(string name, Sort attributeSort, string[] symbols, int[] ranks)
        {
            #region validity checks
            if (string.IsNullOrWhiteSpace(name) || attributeSort == null || symbols == null || ranks == null || symbols.Length == 0 || ranks.Length == 0)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            var name_nodeSort = new Tuple<string, Sort>(name, attributeSort);
            if (rankedAlphabetSortMap.ContainsKey(name_nodeSort))
                throw new AutomataException(AutomataExceptionKind.RankedAlphabet_IsAlreadyDefined);

            if (symbols.Length != ranks.Length)
                throw new AutomataException(AutomataExceptionKind.RankedAlphabet_IsInvalid);

            Dictionary<string, int> idMap = new Dictionary<string, int>(symbols.Length);
            bool containsOneleaf = false;
            int maxrank = 0;
            for (int i = 0; i < symbols.Length; i++)
                if (string.IsNullOrWhiteSpace(symbols[i]) || idMap.ContainsKey(symbols[i]) || ranks[i] < 0)
                    throw new AutomataException(AutomataExceptionKind.RankedAlphabet_IsInvalid);
                else
                {
                    idMap.Add(symbols[i], i);
                    containsOneleaf = (containsOneleaf || ranks[i] == 0);
                    maxrank = (maxrank > ranks[i] ? maxrank : ranks[i]);
                }
            if (!containsOneleaf)
                throw new AutomataException(AutomataExceptionKind.RankedAlphabet_ContainsNoLeaf);
            #endregion

            int K = symbols.Length;

            var fieldNames = new string[K][];
            var fieldSorts = new Sort[K][];
            var testerNames = new string[K];
            var constructors = new Constructor[K];

            for (int i = 0; i < K; i++)
            {
                string symb = symbols[i];
                int arity = ranks[i] + 1;
                fieldNames[i] = new string[arity];
                fieldSorts[i] = new Sort[arity];
                var field_refs = new uint[arity];
                fieldSorts[i][0] = attributeSort;
                for (int j = 0; j < arity; j++)
                    fieldNames[i][j] = symb + "@" + j;
                constructors[i] = Z.z3.MkConstructor(symb, "$is" + symb, fieldNames[i], fieldSorts[i], field_refs);
            }

            Sort datatype = Z.z3.MkDatatypeSort(name, constructors);

            var constrs = new FuncDecl[K];
            var accessors = new FuncDecl[K][];
            var testers = new FuncDecl[K];

            for (int i = 0; i < K; i++)
            {
                constrs[i] = constructors[i].ConstructorDecl;
                accessors[i] = constructors[i].AccessorDecls;
                testers[i] = constructors[i].TesterDecl;
            }

            rankedAlphabetSortMap[name_nodeSort] = datatype;
            FuncDecl acceptor = Z.MkFuncDecl(string.Format("$lang_{0}", name), new Sort[] { Z.IntSort, datatype }, Z.BoolSort);
            Expr[] vars = new Expr[maxrank + 1];
            vars[0] = Z.MkVar(0, attributeSort);
            for (int i = 1; i <= maxrank; i++)
                vars[i] = Z.MkVar((uint)i, datatype);
            var ra = new RankedAlphabet(this, symbols, idMap, datatype, attributeSort, ranks, constrs, accessors, testers, acceptor, vars);
            rankedAlphabetInfoMap[datatype] = ra;
            return ra;
        }

        Dictionary<Tuple<Sort, Sort>, FuncDecl> transformationLookup = new Dictionary<Tuple<Sort, Sort>, FuncDecl>();

        /// <summary>
        /// Gets the generic uninterpreted function symbol of sort (int,A)->B
        /// for transforming trees of sort A into trees of sort B in a given state of sort int.
        /// </summary>
        /// <param name="inputSort">alphabet sort of input trees</param>
        /// <param name="outputSort">alphabet sort of output trees</param>
        public FuncDecl GetTrans(Sort inputSort, Sort outputSort)
        {
            var key = new Tuple<Sort,Sort>(inputSort, outputSort);
            FuncDecl trans;
            if (!transformationLookup.TryGetValue(key, out trans))
            {
                string trans_name = string.Format("$trans_{0}_{1}", inputSort.Name, outputSort.Name);
                trans = Z.MkFuncDecl(trans_name, new Sort[] { Z.IntSort, inputSort }, outputSort);
                transformationLookup[key] = trans;
            }
            return trans;
        }

        /// <summary>
        /// Returns true iff f:(int,A)->B is the generic uninterpreted function symbol 
        /// for transforming trees of sort A to trees of sort B in a given state of sort int.
        /// </summary>
        internal static bool IsTrans(FuncDecl f)
        {
            return f.Name.ToString().StartsWith("$trans"); //could be made more precise
        }

        /// <summary>
        /// Returns true iff f is a tree language acceptor symbol.
        /// </summary>
        internal static bool IsLang(FuncDecl f)
        {
            return f.Name.ToString().StartsWith("$lang"); //could be made more precise
        }

        /// <summary>
        /// Returns true iff the term is a numeral that represents the identity state.
        /// </summary>
        internal static bool IsIdentityState(Expr state)
        {
            return state.ASTKind == Z3_ast_kind.Z3_NUMERAL_AST && ((IntNum)state).Int == -1;
        }

        /// <summary>
        /// Make a tree rule from the input alphabet to the output alphabet.
        /// </summary>
        /// <param name="inputAlphabet">input alphabet</param>
        /// <param name="outputAlphabet">output alphabet</param>
        /// <param name="state">source state</param>
        /// <param name="symbol">a ranked symbol of the input alphabet</param>
        /// <param name="guard">attribute guard</param>
        /// <param name="output">output tree</param>
        public TreeRule MkTreeRule(RankedAlphabet inputAlphabet, RankedAlphabet outputAlphabet, int state, string symbol, Expr guard, Expr output)
        {
            var A = inputAlphabet;
            var B = outputAlphabet;
            var constr = A.GetConstructor(symbol);

            CheckAttribute(guard, A);

            var outp = (output == null ? null : NormalizeOutput(output, A, B, A.GetRank(symbol)));

            return new TreeRule(Z.MkInt(state), constr, guard, outp, A.GetRank(symbol));
        }

        /// <summary>
        /// Make a tree rule from the input alphabet to the output alphabet.
        /// </summary>
        /// <param name="inputAlphabet">input alphabet</param>
        /// <param name="outputAlphabet">output alphabet</param>
        /// <param name="state">source state</param>
        /// <param name="symbol">a ranked symbol of the input alphabet</param>
        /// <param name="guard">attribute guard</param>
        /// <param name="output">output tree</param>
        /// <param name="lookahead">domain restriction states</param>
        public TreeRule MkTreeRule(RankedAlphabet inputAlphabet, RankedAlphabet outputAlphabet, int state, string symbol, Expr guard, Expr output, params int[][] lookahead)
        {
            var A = inputAlphabet;
            var B = outputAlphabet;
            var constr = A.GetConstructor(symbol);

            CheckAttribute(guard, A);

            if (A.GetRank(symbol) != lookahead.Length)
                throw new AutomataException(AutomataExceptionKind.TreeTheory_RankMismatch);

            ExprSet[] la = new ExprSet[lookahead.Length];
            int j = 0;
            for (int i=0;i<lookahead.Length;i++)
            {
                la[i] = new ExprSet();
                for (j = 0; j < lookahead[i].Length; j++)
                {
                    la[i].Add(Z.MkNumeral(lookahead[i][j], Z.IntSort));
                }
            }

            var outp = (output == null ? null : NormalizeOutput(output, A, B, A.GetRank(symbol)));

            return new TreeRule(Z.MkInt(state), constr, guard, outp, la);
        }

        public TreeRule MkTreeAcceptorRule(RankedAlphabet A, int top, string symbol, Expr guard, params int[] bottom)
        {
            int[][] lookahead = new int[bottom.Length][];
            for (int i = 0; i < bottom.Length; i++)
                lookahead[i] = new int[] { bottom[i] };
            return MkTreeRule(A, A, top, symbol, guard, null, lookahead);
        }

        private void CheckAttribute(Expr term, RankedAlphabet A)
        {
            //check that the term does not use any free varibales other than the attribute variable
            foreach (var v in Z.GetVars(term))
                if (!v.Equals(A.AttrVar))
                    throw new AutomataException(AutomataExceptionKind.TreeTheory_UnexpectedVariable);
        }

        //adds identity states when A=B and a child is ouput without transformation
        internal Expr NormalizeOutput(Expr t, RankedAlphabet A, RankedAlphabet B, int rank) 
        {
            if (Z.IsVar(t))
            {
                if (Z.GetVarIndex(t) > rank || !t.Sort.Equals(A.AlphabetSort))
                    throw new AutomataException(AutomataExceptionKind.TreeTheory_UnexpectedVariable);
                else
                    return Z.MkApp(GetTrans(A.AlphabetSort, B.AlphabetSort), identityState, t);
            }
            else if (t.ASTKind == Z3_ast_kind.Z3_APP_AST)
            {
                var f = t.FuncDecl;
                var args = t.Args;
                if (B.ContainsConstructor(f))
                {
                    CheckAttribute(args[0], A);
                    var args1 = new Expr[args.Length];
                    args1[0] = args[0];
                    for (int j = 1; j < args.Length; j++)
                        args1[j] = NormalizeOutput(args[j], A, B, rank);
                    return Z.MkApp(f, args1);
                }
                else if (IsTrans(f))
                {
                    if (args[0].ASTKind != Z3_ast_kind.Z3_NUMERAL_AST || !Z.IsVar(args[1]) || Z.GetVarIndex(args[1]) > rank || Z.GetVarIndex(args[1]) < 1)
                        throw new AutomataException(AutomataExceptionKind.TreeTheory_UnexpectedVariable);
                    else
                        return t;
                }
                else
                {
                    throw new AutomataException(AutomataExceptionKind.TreeTheory_UnexpectedOutput);
                }
            }
            else
            {
                throw new AutomataException(AutomataExceptionKind.TreeTheory_UnexpectedOutput);
            }
        }

        /// <summary>
        /// Make a new tree automaton or tree transducer.
        /// </summary>
        /// <param name="q0">initial state</param>
        /// <param name="inputAlphabet">input alphabet</param>
        /// <param name="outputAlphabet">output alphabet</param>
        /// <param name="rules">rules of the automaton or transducer</param>
        /// <returns></returns>
        public TreeTransducer MkTreeAutomaton(int q0, RankedAlphabet inputAlphabet, RankedAlphabet outputAlphabet, IEnumerable<TreeRule> rules)
        {
            return MkTreeAutomaton(new List<int>(new int[]{q0}), inputAlphabet, outputAlphabet, rules);
        }
        /// <summary>
        /// Make a new tree automaton or tree transducer.
        /// </summary>
        /// <param name="initStates">initial states</param>
        /// <param name="inputAlphabet">input alphabet</param>
        /// <param name="outputAlphabet">output alphabet</param>
        /// <param name="rules">rules of the automaton or transducer</param>
        /// <returns></returns>
        public TreeTransducer MkTreeAutomaton(IEnumerable<int> initStates, RankedAlphabet inputAlphabet, RankedAlphabet outputAlphabet, IEnumerable<TreeRule> rules)
        {
            foreach(var st in initStates)
                if (st < 0)
                    throw new AutomataException(AutomataExceptionKind.TreeTransducer_InvalidStateId);

            var stateList = new List<Expr>();
            var stateSet = new HashSet<Expr>();
            var rulesList = new List<TreeRule>(rules);
            var q0_list = new List<Expr>();
            foreach(var st in initStates)
                q0_list.Add(Z.MkInt(st));

            #region perform basic sanity checks
            foreach (var rule in rulesList)
            {
                if (rule.State.Equals(identityState))
                    throw new AutomataException(AutomataExceptionKind.TreeTransducer_InvalidUseOfIdentityState);
                if (stateSet.Add(rule.state))
                    stateList.Add(rule.state);
            }
            foreach (var rule in rulesList)
                if (!rule.IsTrueForAllStates(st => (stateSet.Contains(st) || st.Equals(identityState))))
                    throw new AutomataException(AutomataExceptionKind.TreeTransducer_InvalidStateId);
            #endregion

            var ta = new TreeTransducer(q0_list, inputAlphabet, outputAlphabet, stateList, rulesList);
            var ta1 = ta.Clean();
            return ta1;
        }
    }
}