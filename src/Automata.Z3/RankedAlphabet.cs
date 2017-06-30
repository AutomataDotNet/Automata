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
    /// Represents a definition of a datatype over a given set of ranked symbols and a given node sort.
    /// </summary>
    public class RankedAlphabet
    {
        internal TreeTheory tt;
        IList<string> symbols;
        internal Dictionary<string, int> idMap;
        Sort alphabetSort;
        Sort nodeSort;
        Dictionary<int, List<FuncDecl>> symbolsOfRank;
        public int[] ranks;
        public FuncDecl[] constructors;
        FuncDecl[][] accessors;
        FuncDecl[] testers;
        FuncDecl acceptor;
        internal Expr[] vars;
        //internal List<TreeRule>[] identityRules;
        FuncDecl trans;        
        internal Expr attrExpr;

        public bool IsSFAcompatible
        {
            get
            {
                if (this.MaxRank > 1)
                    return false;

                if (this.Symbols.Count != 2)
                    return false;

                return true;
            }
        }

        public FuncDecl SFA_Nil
        {
            get
            {
                if (!IsSFAcompatible)
                    throw new AutomataException(AutomataExceptionKind.TreeTransducer_NotSFAcompatible);

                if (ranks[0] == 0)
                    return constructors[0];
                else
                    return constructors[1];
            }
        }

        public FuncDecl SFA_Cons
        {
            get
            {
                if (!IsSFAcompatible)
                    throw new AutomataException(AutomataExceptionKind.TreeTransducer_NotSFAcompatible);

                if (ranks[0] == 0)
                    return constructors[1];
                else
                    return constructors[0];
            }
        }



        public TreeTheory TT
        {
            get { return tt; }
        }

        /// <summary>
        /// Generic language transduction function from (int, AlphabetSort) to AlphabetSort
        /// </summary>
        public FuncDecl Trans
        {
            get { return trans; }
        }


        TreeTransducer emptyAcceptor;
        TreeTransducer fullAcceptor;
        TreeTransducer idAut;

        /// <summary>
        /// Acceptor of the empty language.
        /// </summary>
        public TreeTransducer EmptyAcceptor
        {
            get { return emptyAcceptor; }
        }

        /// <summary>
        /// Acceptor of the full language.
        /// </summary>
        public TreeTransducer FullAcceptor
        {
            get { return fullAcceptor; }
        }

        /// <summary>
        /// Transducer that is the identity mapping for all trees in the language.
        /// </summary>
        public TreeTransducer IdTransducer
        {
            get { return idAut; }
        }

        //public 


        /// <summary>
        /// Generic acceptor symbol that is a predicate over (int, AlphabetSort)
        /// </summary>
        public FuncDecl Acceptor
        {
            get { return acceptor; }
        }

        /// <summary>
        /// Makes the language acceptance condition for the given states and arguments.
        /// For example if states =[{},{q1},{q2,q3},{q2}] and args = [t0,t1,t2,t3] creates the 
        /// conjunction (($lang(q1,t1) and $lang(q2,t2)) and $lang(q3,t2)) and $lang(q2,t3). 
        /// Assumes that the enumerations have the same length.
        /// </summary>
        /// <param name="states">lang states</param>
        /// <param name="args">child subtrees</param>
        /// <returns></returns>
        internal Expr MkLangConj(IEnumerable<ExprSet> states, IEnumerable<Expr> args)
        {
            Expr res = tt.Z.True;
            var e1 = states.GetEnumerator();
            var e2 = args.GetEnumerator();

            while (e1.MoveNext())
            {
                if (!e2.MoveNext())
                    throw new AutomataException(AutomataExceptionKind.InternalError);

                var qs = e1.Current;
                var arg = e2.Current;
                foreach (var q in qs)
                {
                    var t = tt.Z.MkApp(acceptor, q, arg);
                    res = (res.Equals(tt.Z.True) ? t : tt.Z.MkAndDoNotSimplify(new Expr[]{res, t}));
                }
            }
            return res;
        }


        internal RankedAlphabet(
            TreeTheory tt,
            string[] symbols,
            Dictionary<string, int> idMap,
            Sort alphabetSort,
            Sort nodeSort,
            int[] ranks,
            FuncDecl[] constructors,
            FuncDecl[][] accessors,
            FuncDecl[] testers,
            FuncDecl acceptor,
            Expr[] vars
            )
        {
            this.tt = tt;
            this.symbols = new List<string>(symbols).AsReadOnly();
            this.idMap = idMap;
            this.alphabetSort = alphabetSort;
            this.nodeSort = nodeSort;
            this.ranks = ranks;
            this.constructors = constructors;
            this.accessors = accessors;
            this.testers = testers;
            this.acceptor = acceptor;
            this.vars = vars;
            this.trans = tt.GetTrans(alphabetSort, alphabetSort);
            this.emptyAcceptor = TreeTransducer.MkEmpty(this);
            this.fullAcceptor = TreeTransducer.MkFull(this);
            this.idAut = TreeTransducer.MkId(this);

            this.symbolsOfRank = new Dictionary<int, List<FuncDecl>>();
            for (int i = 0; i < ranks.Length; i++)
            {
                var r = ranks[i];
                if (!symbolsOfRank.ContainsKey(r))
                    symbolsOfRank[r] = new List<FuncDecl>();
                symbolsOfRank[r].Add(constructors[i]);
            }

            var attrDomain = tt.Z.MkFreshFuncDecl("_", new Sort[] { nodeSort }, tt.Z.BoolSort);
            this.attrExpr = tt.Z.MkApp(attrDomain, vars[0]);
            tt.Z.AssertAxiom(this.attrExpr, tt.Z.True, vars[0]);
        }

        /// <summary>
        /// Gets the sort of the alphabet.
        /// </summary>
        public Sort AlphabetSort { get { return alphabetSort; } }


        /// <summary>
        /// Make an output term of applying the given state to the given child subtree.
        /// </summary>
        /// <param name="outputAlphabet">the output alphabet sort</param>
        /// <param name="state">the state from which the child is transduced</param>
        /// <param name="child">the accessor of the child, must be a positive integer between 1 and MaxRank</param>
        public Expr MkTrans(RankedAlphabet outputAlphabet, int state, int child)
        {
            var f = tt.GetTrans(AlphabetSort, outputAlphabet.AlphabetSort);
            var s = tt.Z.MkInt(state);
            if (child < 1 || child > MaxRank)
                throw new AutomataException(AutomataExceptionKind.RankedAlphabet_ChildAccessorIsOutOufBounds);
            var x = ChildVar(child);
            var res = tt.Z.MkApp(f, s, x);
            return res;
        }

        /// <summary>
        /// Make a tree with the given constructor, attribute, and subtrees.
        /// </summary>
        public Expr MkTree(string constructor, Expr attribute, params Expr[] subtrees)
        {
            var f = GetConstructor(constructor);
            var r = GetRank(constructor);
            if (subtrees.Length != r)
                throw new AutomataException(AutomataExceptionKind.RankedAlphabet_IvalidNrOfSubtrees);
            for (int i = 0; i < r; i++)
                if (subtrees[i] == null || !tt.Z.GetSort(subtrees[i]).Equals(alphabetSort))
                    throw new AutomataException(AutomataExceptionKind.RankedAlphabet_InvalidSubtree);
            Expr[] attribute_and_subtrees = new Expr[subtrees.Length + 1];
            attribute_and_subtrees[0] = attribute;
            Array.Copy(subtrees, 0, attribute_and_subtrees, 1, subtrees.Length);
            var res = tt.Z.MkApp(f, attribute_and_subtrees);
            return res;
        }

        /// <summary>
        /// Make a tree with the given constructor, attribute, and subtrees.
        /// </summary>
        public Expr MkTree(FuncDecl f, Expr attribute, params Expr[] subtrees)
        {
            int r = ((int)f.Arity)-1;
            if (subtrees.Length != r)
                throw new AutomataException(AutomataExceptionKind.RankedAlphabet_IvalidNrOfSubtrees);
            for (int i = 0; i < r; i++)
                if (subtrees[i] == null || !tt.Z.GetSort(subtrees[i]).Equals(alphabetSort))
                    throw new AutomataException(AutomataExceptionKind.RankedAlphabet_InvalidSubtree);
            Expr[] attribute_and_subtrees = new Expr[subtrees.Length + 1];
            attribute_and_subtrees[0] = attribute;
            Array.Copy(subtrees, 0, attribute_and_subtrees, 1, subtrees.Length);
            var res = tt.Z.MkApp(f, attribute_and_subtrees);
            return res;
        }

        /// <summary>
        /// Make a an attribute term.
        /// </summary>
        public Expr MkAttr(params Expr[] fields)
        {
            var f = tt.Z.GetTupleConstructor(AttrSort);
            if (tt.Z.GetTupleLength(AttrSort) != fields.Length)
                throw new AutomataException(AutomataExceptionKind.RankedAlphabet_InvalidNrOfFields);
            for(int i = 0; i < fields.Length; i++)
                if (fields[i] == null || !tt.Z.GetSort(fields[i]).Equals(tt.Z.GetDomain(f)[i]))
                    throw new AutomataException(AutomataExceptionKind.RankedAlphabet_InvalidField);
            var attr = tt.Z.MkApp(f, fields);
            return attr;
        }

        /// <summary>
        /// Make an acceptor output term for the given symbol and states.
        /// </summary>
        internal Expr MkAcceptorOutput(FuncDecl f, params Expr[] states)
        {
            var args = new Expr[states.Length +1];
            args[0] = vars[0];
            for (int i = 0; i < states.Length; i++)
                args[i+1] = tt.Z.MkApp(trans, states[i], vars[i + 1]);
            var res = tt.Z.MkApp(f, args);
            return res;
        }

        /// <summary>
        /// Gets the attribute sort.
        /// </summary>
        public Sort AttrSort { get { return nodeSort; } }

        /// <summary>
        /// Gets the collection of all the constructor names.
        /// </summary>
        public ICollection<FuncDecl> Symbols { get { return constructors; } }

        internal int GetId(string symbol)
        {
            int i;
            if (idMap.TryGetValue(symbol, out i))
                return i;
            else
                throw new AutomataException(AutomataExceptionKind.RankedAlphabet_UnknownSymbol);
        }

        /// <summary>
        /// Gets the constructor with the given name. Same as GetConstructor(constructorName).
        /// </summary>
        /// <param name="constructorName">the name of the constructor</param>
        public FuncDecl this[string constructorName]
        {
            get
            {
                return GetConstructor(constructorName);
            }
        }

        /// <summary>
        /// Gets the formal variable for the attribute.
        /// </summary>
        public Expr AttrVar { get { return vars[0]; } }

        /// <summary>
        /// The maximum nr of children on any constructor in Sigma. 
        /// </summary>
        public int MaxRank { get { return vars.Length - 1; } }

        /// <summary>
        /// Gets the formal variable for the k'th child subtree.
        /// </summary>
        /// <param name="k">positive integer between 1 and MaxRank</param>
        public Expr ChildVar(int k)
        {
            if (k < 1 || k > MaxRank)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);
            return vars[k];
        }

        /// <summary>
        /// Gets the constructor for the given node symbol.
        /// </summary>
        /// <param name="symbol">function symbol name</param>
        public FuncDecl GetConstructor(string symbol) { return constructors[GetId(symbol)]; }

        /// <summary>
        /// Gets the accessor of the node label for the given node symbol.
        /// </summary>
        /// <param name="symbol">function symbol name</param>
        public FuncDecl GetAttributeAccessor(string symbol) { return accessors[GetId(symbol)][0]; }

        /// <summary>
        /// Gets the predicate for testing that its argument has the given node symbol.
        /// </summary>
        /// <param name="symbol">function symbol name</param>
        public FuncDecl GetTester(string symbol) { return testers[GetId(symbol)]; }

        /// <summary>
        /// Gets the number of children (subtrees) that a node with the given symbol has.
        /// </summary>
        /// <param name="symbol">function symbol name</param>
        public int GetRank(string symbol) { return ranks[GetId(symbol)]; }

        /// <summary>
        /// Gets the accessor of the n'th child.
        /// </summary>
        /// <param name="symbol">function symbol name</param>
        /// <param name="n">must be an integer between 1 and GetChildRank(symbol)</param>
        public FuncDecl GetChildAccessor(string symbol, int n)
        {
            int id = GetId(symbol);
            if (!(1 <= n && n <= ranks[id]))
                throw new AutomataException(AutomataExceptionKind.RankedAlphabet_ChildAccessorIsOutOufBounds);
            return accessors[id][n];
        }

        /// <summary>
        /// Returns true iff the alphabet contains a constructor with the given name.
        /// </summary>
        /// <param name="symbol">constructor name</param>
        /// <returns></returns>
        public bool ContainsConstructor(string symbol)
        {
            return idMap.ContainsKey(symbol);
        }

        /// <summary>
        /// Returns true iff the constructor belongs to the alphabet.
        /// </summary>
        /// <param name="symbol">constructor</param>
        /// <returns></returns>
        public bool ContainsConstructor(FuncDecl symbol)
        {
            return idMap.ContainsKey(symbol.Name.ToString()) && GetConstructor(symbol.Name.ToString()).Equals(symbol);
        }


        /// <summary>
        /// Returns the name of the alphabet sort.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return AlphabetSort.ToString();
        }

        /// <summary>
        /// Make a new acceptor rule where k = lookahead.Length is the rank of the symbol.
        /// </summary>
        /// <param name="state">top state of the rule</param>
        /// <param name="symbol">symbol of the alphabet</param>
        /// <param name="guard">attribute guard</param>
        /// <param name="lookahead">bottom states of the rule</param>
        public TreeRule MkAcceptorRule(int state, string symbol, Expr guard, params int[] lookahead)
        {
            int[][] botStateSets = Array.ConvertAll(lookahead, b => (b == -1 ? null : new int[] { b }));
            return MkAcceptorRule(state, symbol, guard, botStateSets);
        }

        /// <summary>
        /// Make a new acceptor rule where k = lookahead.Length is the rank of the symbol with guard=true.
        /// </summary>
        /// <param name="state">top state of the rule</param>
        /// <param name="symbol">symbol of the alphabet</param>
        /// <param name="lookahead">bottom states of the rule (sate -1 means that all inputs are accepted)</param>
        public TreeRule MkAcceptorRule(int state, string symbol, params int[] lookahead)
        {
            int[][] botStateSets = Array.ConvertAll(lookahead, b => (b == -1 ? null : new int[] { b }));
            return MkAcceptorRule(state, symbol, tt.Z.True, botStateSets);
        }

        /// <summary>
        /// Make a new acceptor rule where k = lookahead.Length is the rank of the symbol.
        /// </summary>
        /// <param name="state">top state of the rule</param>
        /// <param name="symbol">symbol of the alphabet</param>
        /// <param name="guard">attribute guard</param>
        /// <param name="lookahead">bottom state sets of the rule</param>
        public TreeRule MkAcceptorRule(int state, string symbol, Expr guard, int[][] lookahead)
        {
            int symb_id = GetId(symbol);
            int k = ranks[symb_id];
            if (state < 0 || lookahead == null || lookahead.Length != k || 
                Array.Exists(lookahead, B => (B != null && Array.Exists(B, b => b < 0)))) 
                throw new AutomataException(AutomataExceptionKind.RankedAlphabet_InvalidAcceptorRuleArguments);
            FuncDecl func = constructors[symb_id];
            ExprSet[] termSets = new ExprSet[k];
            for (int i = 0; i < k; i++)
            {
                termSets[i] = new ExprSet();
                if (lookahead[i] != null)
                {
                    for (int j = 0; j < lookahead[i].Length; j++)
                        termSets[i].Add(tt.Z.MkInt(lookahead[i][j]));
                }
            }
            var acc = new TreeRule(tt.Z.MkInt(state), func, guard, null, termSets);
            return acc;
        }

        /// <summary>
        /// Make a new acceptor rule where k = lookahead.Length is the rank of the symbol and the guard is true.
        /// </summary>
        /// <param name="state">top state of the rule</param>
        /// <param name="symbol">symbol of the alphabet</param>
        /// <param name="lookahead">bottom state sets of the rule (elements of lookahead may be null)</param>
        public TreeRule MkAcceptorRule(int state, string symbol, int[][] lookahead)
        {
            return MkAcceptorRule(state, symbol, tt.Z.True, lookahead);
        }

        /// <summary>
        /// Make a new tree acceptor. The initial state is the state of the first rule.
        /// </summary>
        /// <param name="rules">rules where output is assumed to be null</param> 
        public TreeTransducer MkTreeAcceptor(params TreeRule[] rules)
        {
            if (rules.Length == 0)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_EmptySetOfRules);

            int q0 = int.Parse(rules[0].state.ToString());
            var ta = tt.MkTreeAutomaton(q0, this, this, rules);
            return ta;
        }

        private int GetMaxStateId(IEnumerable<TreeRule> rules)
        {
            HashSet<Expr> states = new HashSet<Expr>();
            HashSet<Expr> botStates = new HashSet<Expr>();
            int res = -1;
            foreach (var rule in rules)
            {
                int k = tt.Z.GetNumeralInt(rule.state);
                states.Add(rule.state);
                if (k > res)
                    res = k;
                for (int i=0; i < rule.Rank; i++)
                    foreach (var s in rule.Lookahead(i))
                        botStates.Add(s);
            }
            if (!states.IsSupersetOf(botStates))
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_UndefinedState);

            return res;
        }

        private IEnumerable<Tuple<Expr, ExprSet[]>> EnumerateCombinedRules(ConsList<Expr> state_set, FuncDecl func, 
            Func<Expr,FuncDecl,IEnumerable<TreeRule>> GetRules)
        {
            if (state_set.Rest == null)
                foreach (var rule in GetRules(state_set.First, func))
                    yield return new Tuple<Expr, ExprSet[]>(rule.Guard, rule.lookahead);
            else
            {
                foreach (var rule in GetRules(state_set.First, func))
                {
                    var childStateSets = rule.lookahead;
                    foreach (var rule_base in EnumerateCombinedRules(state_set.Rest, func, GetRules)) 
                    {
                        var guard = tt.Z.MkAndSimplify(rule.Guard, rule_base.Item1);
                        if (!guard.Equals(tt.Z.False))
                        {
                            var childStateSets1 = new ExprSet[childStateSets.Length];
                            for (int i = 0; i < childStateSets.Length; i++)
                            {
                                childStateSets1[i] = new ExprSet(childStateSets[i]);
                                childStateSets1[i].AddRange(rule_base.Item2[i]);
                            }
                            yield return new Tuple<Expr, ExprSet[]>(guard, childStateSets1);
                        }
                    }
                }
            }
        }

        public List<FuncDecl> GetSymbols(int rank)
        {
            List<FuncDecl> symbs;
            if (symbolsOfRank.TryGetValue(rank, out symbs))
                return symbs;
            else
                return new List<FuncDecl>(); 
        }
    }
}
