using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Z3;

using Microsoft.Automata;

using Microsoft.Automata.Z3.Internal;

using ExprPairSet = Microsoft.Automata.OrderedSet<Microsoft.Automata.ComparablePair<Microsoft.Z3.Expr, Microsoft.Z3.Expr>>;

using ExprSet = Microsoft.Automata.OrderedSet<Microsoft.Z3.Expr>;

namespace Microsoft.Automata.Z3
{

    /// <summary>
    /// Symbolic tree transducer over ranked alphabets.
    /// </summary>
    public class TreeTransducer
    {
        TreeTheory tt;
        public readonly List<Expr> roots;
        RankedAlphabet inputAlphabet;
        RankedAlphabet outputAlphabet;
        Dictionary<Expr, Dictionary<FuncDecl, List<TreeRule>>> ruleMap;
        List<Expr> stateList;
        internal List<TreeRule> rulesList;
        internal bool allRulesAreAcceptorRules;

        /// <summary>
        /// States having at least one rule of rank 0.
        /// </summary>
        HashSet<Expr> leaves;

        Dictionary<Expr, List<TreeRule>[]> ruleMapByRank;
        List<TreeRule> _emptyTreeRuleList = new List<TreeRule>();

        /// <summary>
        /// Enumerate all rules from the state with the given rank.
        /// </summary>
        public IEnumerable<TreeRule> GetRulesByRank(Expr state, int rank)
        {
            //if (!(0 <= rank && rank < inputAlphabet.MaxRank))
            //    throw new AutomataException(AutomataExceptionKind.RankedAlphabet_RankIsOutOfBounds);

            var rules = ruleMapByRank[state];
            if (rules[rank] == null)
                return _emptyTreeRuleList;
            else
                return rules[rank];
        }

        /// <summary>
        /// Enumerate all rules from the state with the given rank.
        /// </summary>
        public IEnumerable<TreeRule> GetRulesByRank(int rank)
        {
            //if (!(0 <= rank && rank < inputAlphabet.MaxRank))
            //    throw new AutomataException(AutomataExceptionKind.RankedAlphabet_RankIsOutOfBounds);
            foreach (var state in stateList)
            {
                var rules = ruleMapByRank[state];
                if (rules[rank] != null)
                {
                    foreach (var r in rules[rank])
                        yield return r;
                }
            }
        }

        /// <summary>
        /// Enumerate all rules from the state, in increasing order by rank.
        /// </summary>
        public IEnumerable<TreeRule> GetRules(Expr state)
        {
            var rules = ruleMapByRank[state];
            for (int i = 0; i < rules.Length; i++)
                if (rules[i] != null)
                    foreach (var rule in rules[i])
                        yield return rule;
        }


        /// <summary>
        /// Gets the underlying tree theory solver.
        /// </summary>
        public TreeTheory TT { get { return tt; } }

        internal bool IsIdTransducer
        {
            get { return roots[0].Equals(tt.identityState); }
        }

        /// <summary>
        /// Returns true iff all rules are acceptor rules and input alphabet = output alphabet.
        /// </summary>
        public bool IsAcceptor
        {
            get { return allRulesAreAcceptorRules && (inputAlphabet == outputAlphabet); }
        }

        /// <summary>
        /// Returns true if all rules from the given state are acceptors.
        /// </summary>
        public bool IsPureAcceptorState(Expr state)
        {
            if (state.Equals(tt.identityState))
                return false;

            Dictionary<FuncDecl, List<TreeRule>> rules;
            if (!ruleMap.TryGetValue(state, out rules))
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_UndefinedState);

            foreach (var kv in rules)
                foreach (var rule in kv.Value)
                    if (!rule.IsAcceptorRule)
                        return false;

            return true;
        }

        /// <summary>
        /// Returns true if none of the rules from the given state are acceptors.
        /// </summary>
        public bool IsPureTransducerState(Expr state)
        {
            if (state.Equals(tt.identityState))
                return true;

            Dictionary<FuncDecl, List<TreeRule>> rules;
            if (!ruleMap.TryGetValue(state, out rules))
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_UndefinedState);

            foreach (var kv in rules)
                foreach (var rule in kv.Value)
                    if (rule.IsAcceptorRule)
                        return false;
            return true;
        }

        /// <summary>
        /// Gets the rules from the given source state and the given constructor.
        /// </summary>
        /// <param name="state">given state</param>
        /// <param name="symbol">given constructor</param>
        public IList<TreeRule> GetRules(Expr state, FuncDecl symbol)
        {
            if (state.Equals(tt.identityState))
                if (inputAlphabet != outputAlphabet)
                    throw new AutomataException(AutomataExceptionKind.TreeTransducer_InvalidUseOfIdentityState);
                else
                    return inputAlphabet.IdTransducer.ruleMap[state][symbol];

            Dictionary<FuncDecl, List<TreeRule>> func_rules;
            List<TreeRule> rules;
            if (!ruleMap.TryGetValue(state, out func_rules))
            {
                return new List<TreeRule>().AsReadOnly();
            }
            else if (!func_rules.TryGetValue(symbol, out rules))
            {
                return new List<TreeRule>().AsReadOnly();
            }
            else
            {
                return rules.AsReadOnly();
            }
        }


        static IEnumerable<Tuple<Expr, ExprSet[]>> GetSimultaneousLanguageTransitions(ExprSet states, FuncDecl f, Func<Expr, FuncDecl, IEnumerable<TreeRule>> GetRulesTmp, TreeTheory tt)
        {
            ExprSet[] kids = new ExprSet[tt.Z.GetDomain(f).Length - 1]; //arity of f  - 1
            for (int i = 0; i < kids.Length; i++)
                kids[i] = new ExprSet();
            return GetSimultaneousLanguageTransitions1(states.elements, f, tt.Z.True, kids, GetRulesTmp, tt);
        }

        static IEnumerable<Tuple<Expr, ExprSet[]>> GetSimultaneousLanguageTransitions1(ConsList<Expr> states, FuncDecl f, Expr guard, ExprSet[] kids, Func<Expr, FuncDecl, IEnumerable<TreeRule>> GetRulesTmp, TreeTheory tt)
        {
            if (states == null)
                yield return new Tuple<Expr, ExprSet[]>(guard, kids);
            else
            {
                foreach (var rule in GetRulesTmp(states.First, f))
                {
                    var guard1 = tt.Z.MkAndSimplify(guard, rule.Guard);
                    {
                        if (!guard1.Equals(tt.Z.False))
                        {
                            var kids1 = new ExprSet[kids.Length];
                            for (int i = 0; i < kids.Length; i++)
                                kids1[i] = kids[i].Union(rule.Lookahead(i));
                            foreach (var pair in GetSimultaneousLanguageTransitions1(states.Rest, f, guard1, kids1, GetRulesTmp, tt))
                                yield return pair;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates all the states.
        /// </summary>
        public IEnumerable<Expr> GetStates()
        {
            return stateList;
        }

        /// <summary>
        /// Returns true iff state is a root.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool IsRoot(Expr state)
        {
            return roots.Contains(state);
        }

        /// <summary>
        /// Gets the number of states.
        /// </summary>
        public int StateCount
        {
            get
            {
                return stateList.Count;
            }
        }

        /// <summary>
        /// Enumerates all the rules.
        /// </summary>
        public IEnumerable<TreeRule> GetRules()
        {
            return rulesList;
        }

        /// <summary>
        /// Gets the number of rules.
        /// </summary>
        public int RuleCount
        {
            get
            {
                return rulesList.Count;
            }
        }

        /// <summary>
        /// Gets a root. Returns null if there are no roots.
        /// </summary>
        public Expr Root
        {
            get
            {
                if (roots.Count == 0)
                    return null;
                return roots[0];
            }
        }

        /// <summary>
        /// Gets the number of a roots.
        /// </summary>
        public int RootCount
        {
            get
            {
                return roots.Count;
            }
        }

        /// <summary>
        /// Gets the input alphabet of the transducer.
        /// </summary>
        public RankedAlphabet InputAlphabet { get { return inputAlphabet; } }

        /// <summary>
        /// Gets the output alphabet of the transducer.
        /// </summary>
        public RankedAlphabet OutputAlphabet { get { return outputAlphabet; } }


        TreeTransducer(List<Expr> initialStates, RankedAlphabet inputAlphabet, RankedAlphabet outputAlphabet,
            Dictionary<Expr, Dictionary<FuncDecl, List<TreeRule>>> rules, List<Expr> stateList, List<TreeRule> rulesList)
        {
            this.tt = inputAlphabet.tt;
            this.roots = initialStates;
            this.inputAlphabet = inputAlphabet;
            this.outputAlphabet = outputAlphabet;
            this.ruleMap = rules;
            this.stateList = stateList;
            this.rulesList = rulesList;

            foreach (var s in inputAlphabet.Symbols)
                rulesForSymbol[s] = new List<TreeRule>();
            foreach (var r in rulesList)
                rulesForSymbol[r.Symbol].Add(r);
        }

        int isempty = -1;
        /// <summary>
        /// Returns true iff the automaton accepts no inputs.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                if (roots.Count == 0)
                    return true;
                if (rulesList.Count == 0)
                    return true;
                if (isempty == -1) //have not checked yet
                    isempty = (Clean().roots.Count == 0 ? 0 : 1);
                return (isempty == 0);
            }
        }


        //internal static TreeTransducer MkEmpty(RankedAlphabet inputAlphabet)
        //{
        //    var tt = inputAlphabet.tt;
        //    var q0 = tt.Z.MkInt(0);
        //    var ruleMap = new Dictionary<Expr, Dictionary<FuncDecl, List<TreeRule>>>();
        //    ruleMap[q0] = new Dictionary<FuncDecl, List<TreeRule>>();
        //    var stateList = new List<Expr>();
        //    stateList.Add(q0);
        //    var empty = new TreeTransducer(new List<Expr>(new Expr[] { q0 }), inputAlphabet, inputAlphabet, ruleMap, stateList, new List<TreeRule>());
        //    empty.clean = true;
        //    empty.allRulesAreAcceptorRules = true;
        //    empty.determinized = true;
        //    empty.isNormalized = true;
        //    return empty;
        //}

        internal static TreeTransducer MkEmpty(RankedAlphabet inputAlphabet)
        {
            var tt = inputAlphabet.tt;
            var q0 = tt.Z.MkInt(0);
            var ruleMap = new Dictionary<Expr, Dictionary<FuncDecl, List<TreeRule>>>();
            var q0_ruleMap = new Dictionary<FuncDecl, List<TreeRule>>();
            var rulesList = new List<TreeRule>();
            ruleMap[q0] = q0_ruleMap;
            for (int i = 0; i < inputAlphabet.constructors.Length; i++)
            {
                var r = new TreeRule(q0, inputAlphabet.constructors[i], tt.Z.True, null, inputAlphabet.ranks[i]);
                var rs = new List<TreeRule>();
                rs.Add(r);
                q0_ruleMap[r.Symbol] = rs;
                rulesList.Add(r);
            }
            var stateList = new List<Expr>();
            stateList.Add(q0);
            var emp = new TreeTransducer(new List<Expr>(), inputAlphabet, inputAlphabet, ruleMap, stateList, rulesList);
            emp.clean = true;
            emp.allRulesAreAcceptorRules = true;
            emp.determinized = true;
            emp.isNormalized = true;
            return emp;
        }

        internal static TreeTransducer MkFull(RankedAlphabet inputAlphabet)
        {
            var tt = inputAlphabet.tt;
            var q0 = tt.Z.MkInt(0);
            var ruleMap = new Dictionary<Expr, Dictionary<FuncDecl, List<TreeRule>>>();
            var q0_ruleMap = new Dictionary<FuncDecl, List<TreeRule>>();
            var rulesList = new List<TreeRule>();
            ruleMap[q0] = q0_ruleMap;
            for (int i = 0; i < inputAlphabet.constructors.Length; i++)
            {
                var r = new TreeRule(q0, inputAlphabet.constructors[i], tt.Z.True, null, inputAlphabet.ranks[i]);
                var rs = new List<TreeRule>();
                rs.Add(r);
                q0_ruleMap[r.Symbol] = rs;
                rulesList.Add(r);
            }
            var stateList = new List<Expr>();
            stateList.Add(q0);
            var full = new TreeTransducer(new List<Expr>(new Expr[] { q0 }), inputAlphabet, inputAlphabet, ruleMap, stateList, rulesList);
            full.clean = true;
            full.allRulesAreAcceptorRules = true;
            full.determinized = true;
            full.isNormalized = true;
            return full;
        }

        /// <summary>
        /// The only automaton that has -1 as its only state.
        /// </summary>
        /// <param name="A"></param>
        /// <returns></returns>
        internal static TreeTransducer MkId(RankedAlphabet A)
        {
            var tt = A.tt;
            var q0 = tt.identityState;
            var ruleMap = new Dictionary<Expr, Dictionary<FuncDecl, List<TreeRule>>>();
            var q0_ruleMap = new Dictionary<FuncDecl, List<TreeRule>>();
            var rulesList = new List<TreeRule>();
            ruleMap[q0] = q0_ruleMap;
            for (int i = 0; i < A.constructors.Length; i++)
            {
                var args = new Expr[A.ranks[i] + 1];
                for (int j = 0; j < args.Length; j++)
                    args[j] = (j == 0 ? A.vars[0] : tt.Z.MkApp(A.Trans, tt.identityState, A.vars[j]));
                var outp = tt.Z.MkApp(A.constructors[i], args);
                var r = new TreeRule(q0, A.constructors[i], tt.Z.True, outp, A.ranks[i]);
                var rs = new List<TreeRule>();
                rs.Add(r);
                q0_ruleMap[r.Symbol] = rs;
                rulesList.Add(r);
            }
            var stateList = new List<Expr>();
            stateList.Add(q0);
            var id = new TreeTransducer(new List<Expr>(new Expr[] { q0 }), A, A, ruleMap, stateList, rulesList);
            id.clean = true;
            id.allRulesAreAcceptorRules = false;
            return id;
        }

        /// <summary>
        /// Creates a new tree automaton. Sets also the IsAcceptor if all rules are acceptor rules.
        /// </summary>
        internal TreeTransducer(List<Expr> initStates, RankedAlphabet inpAlphabet, RankedAlphabet outpAlphabet,
            List<Expr> stList, IEnumerable<TreeRule> rulesList1)
        {
            this.tt = inpAlphabet.tt;
            this.roots = initStates;
            this.inputAlphabet = inpAlphabet;
            this.outputAlphabet = outpAlphabet;
            this.stateList = stList;
            var rulesList2 = new List<TreeRule>();
            var aldreadyAdded = new HashSet<TreeRule>();
            this.ruleMap = new Dictionary<Expr, Dictionary<FuncDecl, List<TreeRule>>>();
            foreach (var q in stList)
                ruleMap[q] = new Dictionary<FuncDecl, List<TreeRule>>();
            bool isAcc = true;
            //var isAccState = new Dictionary<Expr, bool>();

            this.leaves = new HashSet<Expr>();
            this.ruleMapByRank = new Dictionary<Expr, List<TreeRule>[]>();
            foreach (var q in stList)
                this.ruleMapByRank[q] = new List<TreeRule>[inpAlphabet.MaxRank + 1];

            foreach (var s in inputAlphabet.Symbols)
                rulesForSymbol[s] = new List<TreeRule>();

            foreach (var rule in rulesList1)
            {
                rulesForSymbol[rule.Symbol].Add(rule);
                bool isNew = aldreadyAdded.Add(rule);
                if (isNew) //do not add duplicates
                {
                    if (rule.Rank == 0)
                        leaves.Add(rule.State);

                    List<TreeRule>[] rules = ruleMapByRank[rule.State];
                    if (rules[rule.Rank] == null)
                        rules[rule.Rank] = new List<TreeRule>();
                    rules[rule.Rank].Add(rule);

                    //if (!isAccState.ContainsKey(rule.state))
                    //    isAccState[rule.state] = rule.IsAcceptorRule;
                    //else if (isAccState[rule.state] != rule.IsAcceptorRule)
                    //    throw new AutomataException(AutomataExceptionKind.TreeTransducer_InvalidUseOfAcceptorState);

                    //foreach (var s in rule.EnumerateStatesInOutput())
                    //{
                    //    if (!isAccState.ContainsKey(s.First))
                    //        isAccState[s.First] = false;
                    //    else if (isAccState[s.First])
                    //        throw new AutomataException(AutomataExceptionKind.TreeTransducer_InvalidUseOfAcceptorState);
                    //}

                    rulesList2.Add(rule);
                    isAcc = (isAcc && rule.IsAcceptorRule);
                    var q_rules = ruleMap[rule.State];
                    List<TreeRule> q_rules_f;
                    if (!q_rules.TryGetValue(rule.Symbol, out q_rules_f))
                    {
                        q_rules_f = new List<TreeRule>();
                        q_rules[rule.Symbol] = q_rules_f;
                    }
                    q_rules_f.Add(rule);
                }
            }

            //this.isAcceptorState = isAccState;
            this.rulesList = rulesList2;
            this.allRulesAreAcceptorRules = isAcc;

        }

        //Dictionary<Expr, bool> isAcceptorState = new Dictionary<Expr, bool>();

        //public bool IsAcceptorState(Expr state)
        //{
        //    bool res;
        //    if (isAcceptorState.TryGetValue(state, out res))
        //        return res;
        //    else
        //        throw new AutomataException(AutomataExceptionKind.TreeTransducer_InvalidStateId);
        //}

        /// <summary>
        /// Compose this=A:S->R with B:R->T to AoB:S->T such that (for all x) AoB[x] = B[A[x]].
        /// </summary>
        /// <param name="B">transducer: R -> T</param>
        /// <returns>composed transducer: S -> T</returns>
        public TreeTransducer Compose(TreeTransducer B)
        {
            return ComposeR(this, B);
        }

        /// <summary>
        /// Restrict the input tree language with respect to the tree language accepted by D. 
        /// D must be an acceptor whose alphabet is the input alphabet of this.
        /// </summary>
        /// <param name="D">acceptor over the input alphabet</param>
        public TreeTransducer RestrictDomain(TreeTransducer D)
        {
            if (D.inputAlphabet != inputAlphabet)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_SortMismatch);
            if (!D.IsAcceptor)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_ArgumentIsNotAcceptor);

            var A = this.Clean();
            var dc = D.Clean();

            /*
            var alph = D.inputAlphabet;
            List<TreeRule> new_rules = new List<TreeRule>();
            foreach (var rule in D.Rules)
                new_rules.Add(rule.MkIdRule(alph));
            var D1 = new TreeAutomaton(D.initialState, alph, alph, D.stateList, new_rules);
            
            //var did = TreeAutomaton.RestrictDomain(A,dc);
            var AD = D1.Compose(A);
             */

            var AD = RestrictDomain(A, dc);

            return AD;
        }

        /// <summary>
        /// Restrict the output tree language with respect to the tree language accepted by R. 
        /// R must be an acceptor whose alphabet is the output alphabet of this.
        /// </summary> 
        /// <param name="R">acceptor over the output alphabet</param>
        public TreeTransducer RestrictRange(TreeTransducer R)
        {
            if (R.inputAlphabet != outputAlphabet)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_SortMismatch);
            if (!R.IsAcceptor)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_ArgumentIsNotAcceptor);

            var D = R.Clean();
            var D_id = D.InputAlphabet.IdTransducer.RestrictDomain(D);

            var A = Clean();
            var res = ComposeR(A, D_id);
            return res;
        }

        /// <summary>
        /// Intersect the tree language accepted by this automaton with the tree language accepted by B.
        /// </summary>
        public TreeTransducer Intersect(TreeTransducer B)
        {
            if (!(IsAcceptor && B.IsAcceptor))
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_NotAcceptor);
            if (B.inputAlphabet != inputAlphabet)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_SortMismatch);

            if (this.IsEmpty)
                return this;

            if (B.IsEmpty)
                return B;

            var A = this.Clean();
            var D = B.Clean();
            return RestrictDomain(A, D);
        }

        /// <summary>
        /// Union the tree language accepted by this automaton with the tree language accepted by B.
        /// </summary>
        public TreeTransducer Union(TreeTransducer aut)
        {
            if (!(IsAcceptor && aut.IsAcceptor))
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_NotAcceptor);
            if (aut.inputAlphabet != inputAlphabet)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_SortMismatch);

            var A = this.Clean();
            var B = aut.Clean();

            var newInitSt = new List<Expr>(A.roots);
            var newStates = new List<Expr>(A.stateList);
            var newRules = new List<TreeRule>(A.rulesList);

            var offSet = A.GetMaxStateId() + 1;
            foreach (var state in B.roots)
                newInitSt.Add(TT.Z.MkInt(TT.Z.GetNumeralInt(state) + offSet));
            var substitutionFunction = new Dictionary<Expr, Expr>();
            foreach (var state in B.stateList)
            {
                var newState = TT.Z.MkInt(TT.Z.GetNumeralInt(state) + offSet);
                newStates.Add(newState);
                substitutionFunction[state] = newState;
            }
            foreach (var rule in B.rulesList)
            {
                var newState = TT.Z.MkInt(TT.Z.GetNumeralInt(rule.state) + offSet);
                var newLookahead = new ExprSet[rule.Rank];
                for (int i = 0; i < rule.Rank; i++)
                {
                    var lsing = rule.Lookahead(i);
                    var newLookSing = new OrderedSet<Expr>();
                    //check that there is an el in the lookahed
                    if (lsing.elements != null)
                        foreach (var lcond in lsing.elements)
                            newLookSing.Add(TT.Z.ApplySubstitution(lcond, substitutionFunction));
                    newLookahead[i] = newLookSing;
                }
                Expr newOutput = null;
                if (rule.Output != null)
                    newOutput = TT.Z.ApplySubstitution(rule.Output, substitutionFunction);
                newRules.Add(new TreeRule(newState, rule.Symbol, rule.Guard, newOutput, newLookahead));
            }

            return new TreeTransducer(newInitSt, inputAlphabet, outputAlphabet, newStates, newRules);
        }

        /// <summary>
        /// Assumes that A and B are clean.
        /// </summary>
        static TreeTransducer RestrictDomain(TreeTransducer A, TreeTransducer B)
        {
            A = A.RemoveMultipleInitialStates();
            B = B.RemoveMultipleInitialStates();

            if (!A.IsClean || !B.IsClean)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_ArgumentIsNotClean);

            if (A.IsIdTransducer)
            {
                var alph = B.inputAlphabet;
                List<TreeRule> new_rules = new List<TreeRule>();
                foreach (var rule in B.GetRules())
                    new_rules.Add(rule.MkIdRule(A.inputAlphabet));

                var Arestr = new TreeTransducer(B.roots, alph, alph, B.stateList, new_rules);
                return Arestr;
            }

            var Z = A.tt.Z;
            var tt = A.tt;

            var statePair2Id = new Dictionary<Tuple<Expr, Expr>, Expr>();

            var init_states_pair_list = new List<Tuple<Expr, Expr>>();
            var init_states_term_list = new List<Expr>();
            var counter = 0;
            foreach (var inStA in A.roots)
                foreach (var inStB in B.roots)
                {
                    var instpair = new Tuple<Expr, Expr>(inStA, inStB);
                    var instid = Z.MkInt(counter);
                    init_states_pair_list.Add(instpair);
                    init_states_term_list.Add(instid);
                    statePair2Id[instpair] = instid;
                    counter++;
                }



            var stack = new Stack<Tuple<Expr, Expr>>(init_states_pair_list);
            var stateList = new List<Expr>(init_states_term_list);
            var rulesMap = new Dictionary<Expr, Dictionary<FuncDecl, List<TreeRule>>>();
            var rulesList = new List<TreeRule>();

            Expr qID = tt.identityState;

            #region helpers

            Action<TreeRule> AddRule = rule =>
            {
                Dictionary<FuncDecl, List<TreeRule>> func_rules;
                if (!rulesMap.TryGetValue(rule.State, out func_rules))
                {
                    func_rules = new Dictionary<FuncDecl, List<TreeRule>>();
                    rulesMap[rule.State] = func_rules;
                }
                List<TreeRule> rs;
                if (!func_rules.TryGetValue(rule.Symbol, out rs))
                {
                    rs = new List<TreeRule>();
                    func_rules[rule.Symbol] = rs;
                }
                rs.Add(rule);
                rulesList.Add(rule);
                if ((rulesList.Exists(x => x.state == rule.state && x.IsAcceptorRule)) && (rulesList.Exists(x => x.state == rule.state && !x.IsAcceptorRule)))
                    throw new AutomataException(AutomataExceptionKind.TreeTransducer_ArgumentIsNotAcceptor);
            };

            int id = 1;

            Func<Expr, Expr, Expr> GetStateId = (p, q) =>
            {
                if (p.Equals(qID) && q.Equals(qID))
                    return qID;

                var pq = new Tuple<Expr, Expr>(p, q);

                Expr pq_id;
                if (statePair2Id.TryGetValue(pq, out pq_id))
                    return pq_id;
                else
                {
                    pq_id = tt.Z.MkInt(id++);
                    statePair2Id[pq] = pq_id;
                    stateList.Add(pq_id);
                    stack.Push(pq);
                }
                return pq_id;
            };
            #endregion

            //DFS algorithm for restricting the domain of A with B
            //stack contains the unexlpored state combinations
            while (stack.Count > 0)
            {
                var qA_qB_pair = stack.Pop();
                var qAB = statePair2Id[qA_qB_pair];
                var qA = qA_qB_pair.Item1;
                var qB = qA_qB_pair.Item2;
                foreach (var F in A.inputAlphabet.constructors)
                    foreach (var ruleA in A.GetRules(qA, F))
                    {
                        foreach (var ruleB in B.GetRules(qB, F))
                        {
                            var guard = Z.MkAndSimplify(ruleA.Guard, ruleB.Guard);
                            if (!guard.Equals(Z.False))
                            {
                                ExprSet[] lookahead = new ExprSet[ruleA.Rank];
                                for (int i = 0; i < lookahead.Length; i++)
                                {
                                    var qA_i = (ruleA.Lookahead(i).IsEmpty ? qID : ruleA.Lookahead(i).SomeElement);
                                    var qB_i = (ruleB.Lookahead(i).IsEmpty ? qID : ruleB.Lookahead(i).SomeElement);
                                    var qAB_i = GetStateId(qA_i, qB_i);
                                    lookahead[i] = (qAB_i.Equals(qID) ? new ExprSet() : new ExprSet(qAB_i));
                                }
                                //specialize the output of A well when it is not null
                                Func<Expr, Expr> subst = t =>
                                {
                                    if (!(t.ASTKind == Z3_ast_kind.Z3_APP_AST && TreeTheory.IsTrans(t.FuncDecl)))
                                        return null;

                                    Expr qA_i = t.Args[0];
                                    int i = ((int)tt.Z.GetVarIndex(t.Args[1])) - 1;
                                    var qB_i = (ruleB.Lookahead(i).IsEmpty ? qID : ruleB.Lookahead(i).SomeElement);
                                    var qAB_i = GetStateId(qA_i, qB_i);
                                    return tt.Z.MkApp(t.FuncDecl, qAB_i, t.Args[1]);
                                };
                                Expr outp1 = (ruleA.Output == null ? null : tt.Z.Simplify(tt.Z.RewriteExpr(ruleA.Output, subst)));
                                var r = new TreeRule(qAB, F, guard, outp1, lookahead);
                                AddRule(r);
                            }
                        }
                    }
            }
            var AxB = new TreeTransducer(init_states_term_list, A.inputAlphabet, A.outputAlphabet, stateList, rulesList);
            AxB.CheckInvariant();
            if (AxB.IsEmpty)
            {
                AxB.clean = true;
                return AxB;
            }
            var cfg = AxB.ToCFG();
            var aliveset = cfg.GetUsefulNonterminals(true);
            Predicate<Expr> IsUselessState = state =>
            {
                return !aliveset.Contains(((IntNum)state).Int.ToString()) && !state.Equals(tt.identityState);
            };

            rulesList.RemoveAll(r => (!(r.IsTrueForAllStates(s => !IsUselessState(s)))));
            stateList.RemoveAll(IsUselessState);

            if (rulesList.Count == 0)
                return A.inputAlphabet.EmptyAcceptor;

            var AxB_clean = new TreeTransducer(init_states_term_list, A.inputAlphabet, A.outputAlphabet, stateList, rulesList);
            AxB_clean.clean = true;
            //AxB_clean.CheckInvariant();
            return AxB_clean;
        }

        /// <summary>
        /// Returns all transduced output trees of the given input trees. 
        /// This is an eager version of Apply.
        /// </summary>
        /// <param name="inputs">given input trees</param>
        public Expr[] this[params Expr[] inputs]
        {
            get
            {
                return new List<Expr>(Apply(inputs)).ToArray();
            }
        }

        /// <summary>
        /// Returns true iff the automaton accepts the given input tree.
        /// </summary>
        /// <param name="input">tree over the input alphabet</param>
        public bool Accepts(Expr input)
        {
            foreach (var tmp in this.Apply(new Expr[] { input }))
                return true;
            return false;
        }

        /// <summary>
        /// Enumerates all transduced output trees from the given enumeration of concrete input trees.
        /// </summary>
        /// <param name="inputs">given enumeration of input trees</param>
        public IEnumerable<Expr> Apply(IEnumerable<Expr> inputs)
        {
            var tc = new Reducer(this, (x, y) => { throw new AutomataException(AutomataExceptionKind.InvalidArgument); });
            foreach (var t in inputs)
                foreach (var initSt in roots)
                    foreach (var kv in tc.Reduce(new ReductionValue(tt.Z.True, tt.Z.MkApp(tt.GetTrans(
                          this.inputAlphabet.AlphabetSort,
                          this.outputAlphabet.AlphabetSort), initSt, t))))
                        yield return (kv.output == null ? null : tt.Z.Simplify(kv.output));
        }

        /*
        /// <summary>
        /// !!!! depricated !!!!
        /// Compose A:S->R with B:R->T to AoB:S->T such that (for all x) AoB[x] = B[A[x]].
        /// </summary>
        /// <param name="A">transducer: S -> R</param>
        /// <param name="B">transducer: R -> T</param>
        /// <returns>composed transducer: S -> T</returns>
        static TreeAutomaton Compose(TreeAutomaton A, TreeAutomaton B)
        {
            if (A.tt != B.tt)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_SolverMismatch);

            var tt = A.tt;

            var S = A.inputAlphabet;
            var T = B.outputAlphabet;

            if (A.outputAlphabet != B.inputAlphabet)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_SortMismatch);

            var R = A.outputAlphabet;

            var Z = tt.Z;
            var trans_S_T = tt.GetTrans(S.AlphabetSort, T.AlphabetSort);
            var trans_R_T = tt.GetTrans(R.AlphabetSort, T.AlphabetSort);

            var q0A_q0B_pair = new Tuple<Expr, Expr>(A.initialState, B.initialState);
            var q0AB = Z.MkInt(0);
            var statePair2Id = new Dictionary<Tuple<Expr, Expr>, Expr>();
            statePair2Id[q0A_q0B_pair] = q0AB;
            var stack = new Stack<Tuple<Expr, Expr>>(statePair2Id.Keys);
            var stateList = new List<Expr>(new Expr[] { q0AB });
            var rulesMap = new Dictionary<Expr, Dictionary<FuncDecl, List<TreeRule>>>();
            var rulesList = new List<TreeRule>();

            #region helpers

            Action<TreeRule> AddRule = rule =>
            {
                Dictionary<FuncDecl, List<TreeRule>> func_rules;
                if (!rulesMap.TryGetValue(rule.State, out func_rules))
                {
                    func_rules = new Dictionary<FuncDecl, List<TreeRule>>();
                    rulesMap[rule.State] = func_rules;
                }
                List<TreeRule> rs;
                if (!func_rules.TryGetValue(rule.Symbol, out rs))
                {
                    rs = new List<TreeRule>();
                    func_rules[rule.Symbol] = rs;
                }
                rs.Add(rule);
                rulesList.Add(rule);
            };

            int id = 1;
            Func<Expr, Expr, Tuple<Expr, Expr>> Tuple = ((p, q) => new Tuple<Expr, Expr>(p, q));
            Func<Tuple<Expr, Expr>, Expr> Pair2Id = pq =>
            {
                if (pq.First.Equals(tt.identityState) && pq.Second.Equals(tt.identityState))
                    return tt.identityState;  //idempotent, it is never pushed to stack

                Expr pq_id;
                if (statePair2Id.TryGetValue(pq, out pq_id))
                    return pq_id;
                else
                {
                    pq_id = tt.Z.MkInt(id++);
                    statePair2Id[pq] = pq_id;
                    stateList.Add(pq_id);
                    stack.Push(pq);
                }
                return pq_id;
            };
            #endregion

            //DFS algorithm for composing A with B
            //stack contains the unexlpored state combinations
            while (stack.Count > 0)
            {
                var qA_qB_pair = stack.Pop();
                var qAB = statePair2Id[qA_qB_pair];
                var qA = qA_qB_pair.First;
                var qB = qA_qB_pair.Second;
                if (qA.Equals(tt.identityState)) //this case is possible only when S=R
                {
                    #region A behaves as an identity function
                    //this is the special identity transformation state in A, in particular this implies that S=R
                    foreach (var func_rulesB in B.ruleMap[qB])
                    {
                        foreach (var ruleB in func_rulesB.Value)
                        {
                            Func<Expr, Expr> StateRenamer = t =>
                            {
                                if (t.ASTKind == Z3_ast_kind.Z3_APP_AST)
                                {
                                    var f = t.FuncDecl;
                                    var args = t.Args;
                                    if (TreeTheory.IsTrans(f))
                                        return Z.MkApp(trans_S_T, Pair2Id(Tuple(tt.identityState, args[0])), args[1]);
                                    else
                                        return null;
                                }
                                else
                                    return t;
                            };
                            var ruleAB = new TreeRule(qAB, ruleB.Symbol, ruleB.Guard, Z.RewriteExpr(ruleB.Output, StateRenamer), ruleB.Rank);
                            AddRule(ruleAB);
                        }
                    }
                    #endregion
                }
                else if (qB.Equals(tt.identityState)) //this case is possible only when R=T
                {
                    #region B behaves as an identity function
                    //this is the special identity transformation state in B, in particular this implies that R=T
                    foreach (var func_rulesA in A.ruleMap[qA])
                    {
                        foreach (var ruleA in func_rulesA.Value)
                        {
                            Func<Expr, Expr> StateRenamer = t =>
                            {
                                if (t.ASTKind == Z3_ast_kind.Z3_APP_AST)
                                {
                                    var f = t.FuncDecl;
                                    var args = t.Args;
                                    if (TreeTheory.IsTrans(f))
                                        return Z.MkApp(trans_S_T, Pair2Id(Tuple(args[0], tt.identityState)), args[1]);
                                    else
                                        return null;
                                }
                                else
                                    return t;
                            };
                            var ruleAB = new TreeRule(qAB, ruleA.Symbol, ruleA.Guard, Z.RewriteExpr(ruleA.Output, StateRenamer), ruleA.Rank);
                            AddRule(ruleAB);
                        }
                    }
                    #endregion
                }
                else
                {
                    foreach (var func_rulesA in A.ruleMap[qA])
                    {
                        var F = func_rulesA.Key;
                        var rulesA = func_rulesA.Value;
                        foreach (var ruleA in rulesA)
                        {
                            #region compose the ruleA for input symbol F with all rules of B
                            var tc = new TransductionComposer(tt, (aState, bState) => Pair2Id(Tuple(aState, bState)), B.GetRules);
                            var mixedExpr = Z.MkApp(trans_R_T, qB, ruleA.Output);
                            foreach (var guard_output in tc.Reduce(ruleA.Guard, mixedExpr))
                            {
                                AddRule(new TreeRule(qAB, F, Z.MkAnd(ruleA.Guard, guard_output.First), guard_output.Second, ruleA.Rank));
                            }
                            #endregion
                        }
                    }
                }
            }

            var qidAB = tt.identityState;
            var AB = new TreeAutomaton(q0AB, S, T, rulesMap, stateList, rulesList);

            return AB;
        }



        */

        /// <summary>
        /// Compose A:S->R with B:R->T to AoB:S->T such that, for all x, AoB(x) = B(A(x)).
        /// </summary>
        /// <param name="A">transducer: S -> R</param>
        /// <param name="B">transducer: R -> T</param>
        /// <returns>composed transducer: S -> T</returns>
        public static TreeTransducer ComposeR(TreeTransducer A, TreeTransducer B)
        {
            A = A.RemoveMultipleInitialStates();
            B = B.RemoveMultipleInitialStates();

            if (A.tt != B.tt)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_SolverMismatch);
            if (A.outputAlphabet != B.inputAlphabet)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_SortMismatch);

            if (A == B.inputAlphabet.IdTransducer)
                return B;

            if (B == A.outputAlphabet.IdTransducer)
                return A;

            var tt = A.tt;
            var Z = tt.Z;

            var trans_AoB = tt.GetTrans(A.inputAlphabet.AlphabetSort, B.outputAlphabet.AlphabetSort);
            var trans_B = tt.GetTrans(B.inputAlphabet.AlphabetSort, B.outputAlphabet.AlphabetSort);

            //include all rules from A and convert them to acceptor rules
            //these rules are used in the composed lookahead

            var A_acc = A.ComputeDomainAcceptor(false);
            var rulesList = new List<TreeRule>(A_acc.rulesList);

            int new_id = A.GetMaxStateId() + 1; //may not use state id's of A_acc as new ids

            var init_states_pair_list = new List<Tuple<Expr, Expr>>();
            var init_states_term_list = new List<Expr>();

            var statePair2Id = new Dictionary<Tuple<Expr, Expr>, Expr>();
            foreach (var inStA in A.roots)
                foreach (var inStB in B.roots)
                {
                    var instpair = new Tuple<Expr, Expr>(inStA, inStB);
                    var instid = Z.MkInt(new_id++);
                    init_states_pair_list.Add(instpair);
                    init_states_term_list.Add(instid);
                    statePair2Id[instpair] = instid;
                }


            var stack = new Stack<Tuple<Expr, Expr>>();
            foreach (var stpair in init_states_pair_list)
                stack.Push(stpair);

            //states of A are reused inside the acceptor rules from A
            var stateList = new List<Expr>(A.stateList);
            stateList.AddRange(init_states_term_list);
            var rulesMap = new Dictionary<Expr, Dictionary<FuncDecl, List<TreeRule>>>();

            //product states 
            //var stack2 = new Stack<Tuple<ExprSet, ExprSet>>();
            //var idMap2 = new Dictionary<Tuple<ExprSet, ExprSet>, Expr>();

            #region helpers

            Action<TreeRule> AddRule = rule =>
            {
                #region add a new rule
                Dictionary<FuncDecl, List<TreeRule>> func_rules;
                if (!rulesMap.TryGetValue(rule.State, out func_rules))
                {
                    func_rules = new Dictionary<FuncDecl, List<TreeRule>>();
                    rulesMap[rule.State] = func_rules;
                }
                List<TreeRule> rs;
                if (!func_rules.TryGetValue(rule.Symbol, out rs))
                {
                    rs = new List<TreeRule>();
                    func_rules[rule.Symbol] = rs;
                }
                rs.Add(rule);
                rulesList.Add(rule);
                #endregion
            };

            Func<Expr, Expr, Expr> MkStateId = (a, b) =>
            {
                #region make a state id for the pair (a, b)
                var ab = new Tuple<Expr, Expr>(a, b);

                Expr ab_id;
                if (statePair2Id.TryGetValue(ab, out ab_id))
                    return ab_id;
                else
                {
                    ab_id = (a.Equals(tt.identityState) && b.Equals(tt.identityState) ? tt.identityState : tt.Z.MkInt(new_id++));
                    statePair2Id[ab] = ab_id;
                    if (!ab_id.Equals(tt.identityState))
                    {
                        stateList.Add(ab_id);
                        stack.Push(ab);
                    }
                }
                return ab_id;
                #endregion
            };

            #endregion

            var AoBreducer = new Reducer(B, MkStateId);

            //DFS algorithm for composing A with B
            //stack contains the unexlpored transducer state combinations
            while (stack.Count > 0)
            {
                var qA_qB_pair = stack.Pop();
                var qAB = statePair2Id[qA_qB_pair];

                var qA = qA_qB_pair.Item1;
                var qB = qA_qB_pair.Item2;

                var func_rulesA_from_qA = (qA.Equals(tt.identityState) ? A.inputAlphabet.IdTransducer.ruleMap[qA] : A.ruleMap[qA]);
                foreach (var func_rulesA in func_rulesA_from_qA)
                {
                    var F = func_rulesA.Key;
                    var rulesA = func_rulesA.Value;
                    foreach (var ruleA in rulesA)
                    {
                        #region compose the ruleA for input symbol F with all rules of B

                        var output = (ruleA.IsAcceptorRule ? null : Z.MkApp(trans_B, qB, ruleA.Output));

                        foreach (var rv in AoBreducer.Reduce(new ReductionValue(ruleA.Guard, output, ruleA.Rank)))
                        {
                            ExprSet[] lookahead = new ExprSet[ruleA.Rank];
                            for (int i = 0; i < ruleA.Rank; i++)
                            {
                                //the states of A are used as-is in the lookahead
                                var s = ruleA.Lookahead(i).Union(rv.given[i]);
                                lookahead[i] = s;
                            }
                            AddRule(new TreeRule(qAB, F, rv.guard, rv.output, lookahead));
                        }

                        #endregion
                    }
                }
            }

            var AB = new TreeTransducer(init_states_term_list, A.inputAlphabet, B.outputAlphabet, stateList, rulesList);
            var AB1 = AB.Clean();
            return AB1;
        }

        private bool IsBasicOutput(Expr term)
        {
            var kind = term.ASTKind;
            if (kind == Z3_ast_kind.Z3_APP_AST)
            {
                if (TreeTheory.IsTrans(term.FuncDecl))
                    return false;
                else
                    return Array.TrueForAll(term.Args, IsBasicOutput);
            }
            else
                return true;
        }

        /// <summary>
        /// Gets the maximum state id used in the automaton.
        /// </summary>
        public int GetMaxStateId()
        {
            int res = 0;
            foreach (var s in stateList)
            {
                int k = tt.Z.GetNumeralInt(s);
                if (k > res)
                    res = k;
            }
            return res;
        }

        /// <summary>
        /// Generate a witness for the domain of the language, return null if the language is empty
        /// </summary>
        public Expr GenerateWitness(bool cleanit = true)
        {
            if (!this.IsAcceptor)
            {
                var A = this.ComputeDomainAcceptor(cleanit);
                return A.GenerateWitness(false);
            }
            else if (!this.IsClean)
            {
                var A = this.Clean();
                return A.GenerateWitness(false);
            }
            //top down search
            return GenerateSomeWitnessFromState(Root, null, new Dictionary<Expr, Expr>());

            #region old code
            //Dictionary<Expr, Expr> treeOfState = new Dictionary<Expr, Expr>();

            //HashSet<Expr> toReachStates = new HashSet<Expr>(this.stateList);
            //HashSet<Expr> reachedStates = new HashSet<Expr>();

            //Expr constTree = null;

            //for (int i = 0; i < inputAlphabet.ranks.Length; i++)
            //{
            //    var rank = inputAlphabet.ranks[i];
            //    var symb = inputAlphabet.constructors[i];
            //    bool changed = true; ;
            //    while (changed)
            //    {
            //        changed = false;
            //        foreach (var state in toReachStates)
            //        {
            //            List<TreeRule> rules;
            //            if (this.ruleMap[state].TryGetValue(symb, out rules))
            //                foreach (var rule in this.ruleMap[state][symb])
            //                {
            //                    //Build tree
            //                    bool isReady = true;
            //                    Expr[] subtrees = new Expr[rank];
            //                    for (int j = 0; j < rank; j++)
            //                    {
            //                        if (rule.lookahead[j].IsEmpty)
            //                            subtrees[j] = constTree;
            //                        foreach (var st in rule.lookahead[j])
            //                        {
            //                            if (!reachedStates.Contains(st))
            //                            {
            //                                isReady = false;
            //                                break;
            //                            }
            //                            subtrees[j] = treeOfState[st];
            //                        }
            //                        if (!isReady)
            //                            break;
            //                    }
            //                    //All rhs is fine
            //                    if (isReady)
            //                    {
            //                        Expr t1 = rule.guard;
            //                        if (tt.Z.IsGround(rule.guard))
            //                            t1 = inputAlphabet.attrExpr;

            //                        var treeAttr = tt.Z.FindOneMember(t1).Value;
            //                        Expr tree = inputAlphabet.MkTree(symb.Name.ToString(), treeAttr, subtrees);
            //                        //For -1 state
            //                        if (constTree == null)
            //                            constTree = tree;

            //                        reachedStates.Add(state);
            //                        changed = true;
            //                        if (this.initialStates.Contains(state))
            //                            return tree;

            //                        treeOfState[state] = tree;
            //                        break;
            //                    }
            //                }
            //        }
            //        foreach (var reachedState in reachedStates)
            //            toReachStates.Remove(reachedState);
            //    }
            //}
            #endregion
        }

        private Expr GenerateSomeWitnessFromState(Expr q, ConsList<Expr> parents, Dictionary<Expr, Expr> witnesses)
        {
            if (witnesses.ContainsKey(q))
                return witnesses[q];
            else if (parents != null && parents.Exists(q.Equals)) //avoid loops
                return null;
            else
            {
                var parents1 = new ConsList<Expr>(q, parents);
                Expr tree = null;
                foreach (var rule in GetRules(q))
                {
                    bool try_next_rule = false;
                    if (rule.Rank == 0)
                    {
                        Expr treeAttr;
                        if (rule.Guard.Equals(this.tt.Z.True))
                            treeAttr = tt.Z.MainSolver.FindOneMember(this.tt.Z.MkEq(inputAlphabet.AttrVar, inputAlphabet.AttrVar)).Value;
                        else
                            treeAttr = tt.Z.MainSolver.FindOneMember(rule.Guard).Value;
                        tree = inputAlphabet.MkTree(rule.Symbol, treeAttr);
                        break;
                    }
                    else
                    {
                        Expr[] subtrees = new Expr[rule.Rank];
                        for (int i = 0; i < rule.Rank; i++)
                        {
                            if (!rule.Lookahead(i).IsEmptyOrSingleton)
                                throw new AutomataException(AutomataExceptionKind.TreeTransducer_NotClean);
                            if (rule.Lookahead(i).IsEmpty)
                                subtrees[i] = inputAlphabet.MkTree(PickFirstElement(inputAlphabet.GetSymbols(0)), inputAlphabet.tt.Z.True);
                            subtrees[i] = GenerateSomeWitnessFromState(rule.Lookahead(i).SomeElement, parents1, witnesses);
                            if (subtrees[i] == null)
                            {
                                try_next_rule = true;
                                break;
                            }
                        }
                        if (try_next_rule)
                            continue;
                        else
                        {
                            Expr treeAttr;
                            if (rule.Guard.Equals(this.tt.Z.True))
                                treeAttr = tt.Z.MainSolver.FindOneMember(this.tt.Z.MkEq(inputAlphabet.AttrVar, inputAlphabet.AttrVar)).Value;
                            else
                                treeAttr = tt.Z.MainSolver.FindOneMember(rule.Guard).Value;
                            tree = inputAlphabet.MkTree(rule.Symbol, treeAttr, subtrees);
                            break;
                        }
                    }
                }
                if (tree != null)
                    witnesses[q] = tree;
                return tree;
            }
        }

        T PickFirstElement<T>(IEnumerable<T> elems)
        {
            var elem_enumerator = elems.GetEnumerator();
            elem_enumerator.MoveNext();
            return elem_enumerator.Current;
        }


        bool isNormalized = false;
        /// <summary>
        /// Add all states that occur in the output also into the lookahead.
        /// </summary>
        /// <returns></returns>
        public TreeTransducer Normalize()
        {
            if (isNormalized)
                return this;
            List<TreeRule> new_rules = new List<TreeRule>();
            foreach (var rule in this.rulesList)
                new_rules.Add(rule.Normalize());
            var res = new TreeTransducer(roots, inputAlphabet, outputAlphabet, stateList, new_rules);
            res.isNormalized = true;
            res.clean = this.clean;
            res.complete = this.complete;
            res.determinized = this.determinized;
            return res;
        }

        /// <summary>
        /// Normalize, omit all outputs, and clean.
        /// </summary>
        public TreeTransducer ComputeDomainAcceptor(bool cleanIt = true)
        {
            List<TreeRule> new_rules = new List<TreeRule>();
            foreach (var rule in rulesList)
                new_rules.Add(rule.GetAcceptorRule());
            var acc = new TreeTransducer(roots, inputAlphabet, inputAlphabet, stateList, new_rules);
            if (cleanIt)
                return acc.Clean();
            else
                return acc;
        }

        /// <summary>
        /// Abstract as a context free grammar. 
        /// Nonterminals are string names of states. Symbols are '_'. 
        /// Input subtrees accepted by all states are terminals '_'.
        /// </summary>
        public Grammars.ContextFreeGrammar ToCFG()
        {
            var terminal = new Grammars.Terminal<string>("_");

            List<Grammars.Production> cfg_prods = new List<Grammars.Production>();
            foreach (var rule in rulesList)
            {
                var rhs_list = new List<Grammars.GrammarSymbol>();
                rhs_list.Add(new Grammars.Terminal<string>(rule.Symbol.Name.ToString()));
                for (int i = 0; i < rule.Rank; i++)
                    foreach (var q in rule.Lookahead(i))
                        rhs_list.Add(Grammars.Nonterminal.MkNonterminalForZ3Expr(q.ToString()));

                foreach (var q in rule.EnumerateStatesInOutput())
                {
                    if (q.Item1.Equals(tt.identityState))
                        rhs_list.Add(terminal);
                    else
                        rhs_list.Add(Grammars.Nonterminal.MkNonterminalForZ3Expr(q.Item1.ToString()));
                }

                cfg_prods.Add(new Grammars.Production(Grammars.Nonterminal.MkNonterminalForZ3Expr(rule.State.ToString()), rhs_list.ToArray()));
            }
            //Add initial production from dummy initial state to all initial states
            var maxId = GetMaxStateId() + 1;
            var init = Grammars.Nonterminal.MkNonterminalForStateId(maxId);
            foreach (var state in roots)
                cfg_prods.Add(new Grammars.Production(init, new Grammars.GrammarSymbol[] { Grammars.Nonterminal.MkNonterminalForZ3Expr(state.ToString()) }));
            var cfg = new Grammars.ContextFreeGrammar(init, cfg_prods);
            return cfg;
        }


        /// <summary>
        /// In a clean automaton: 
        /// 1) all lookahead state sets are either empty or singleton sets;
        /// 2) if Lookahead(i) = Q and (p,i) occurs in Output then Language(Q) is a subset of Language(p);
        /// 3) all guards are satisfiable;
        /// 4) all states are reachable from the initial state;
        /// 5) all states q are alive (Language(q) is nonempty).
        /// </summary>
        public bool IsClean { get { return clean; } }
        bool clean = false;


        /// <summary>
        /// The result is a clean automaton: 
        /// 1) all lookahead state sets are either empty or singleton sets;
        /// 2) if Lookahead(i) = Q and (p,i) occurs in Output then Language(Q) is a subset of Language(p);
        /// 3) all guards are satisfiable;
        /// 4) all states are reachable from the initial state;
        /// 5) all states q are alive (Language(q) is nonempty).
        /// </summary>
        public TreeTransducer Clean()
        {
            if (clean)
                return this;

            var normalizedStatesetLookup = new Dictionary<ExprSet, ExprSet>();
            var new_rules = new List<TreeRule>();
            var stack = new Stack<ExprSet>();
            var new_states = new List<Expr>(stateList);

            int id = GetMaxStateId() + 1;
            Func<ExprSet, ExprSet> GetNormalizedState = stateset =>
            {
                if (stateset.IsEmptyOrSingleton)
                    return stateset;

                ExprSet s;
                if (!normalizedStatesetLookup.TryGetValue(stateset, out s))
                {
                    s = new ExprSet(tt.Z.MkInt(id++));
                    normalizedStatesetLookup[stateset] = s;
                    stack.Push(stateset);
                    new_states.Add(s.SomeElement);
                }
                return s;
            };

            HashSet<Expr> added = new HashSet<Expr>();
            //normalize the existing rules
            //by replacing multi-state lookahead states with new singletons
            foreach (var rule in rulesList)
            {
                TreeRule new_rule = rule;
                if (!rule.LookaheadIsFlat)
                {
                    var lookahead = Array.ConvertAll(rule.lookahead, x => GetNormalizedState(x));
                    new_rule = new TreeRule(rule.state, rule.Symbol, rule.Guard, rule.Output, lookahead);
                }
                new_rules.Add(new_rule);
            }

            //explore the non-singleton state sets
            //these are additional acceptor rules for the lookaheads
            while (stack.Count > 0)
            {
                var stateset = stack.Pop();
                var st = normalizedStatesetLookup[stateset];
                for (int F_id = 0; F_id < inputAlphabet.constructors.Length; F_id++)
                {
                    var F = inputAlphabet.constructors[F_id];
                    foreach (var acc in EnumerateSimltaneousAcceptors(stateset.elements, new AcceptorBase(F, tt.Z.True, inputAlphabet.ranks[F_id])))
                    {
                        var lookahead = Array.ConvertAll(acc.lookahead, x => GetNormalizedState(x));
                        var rule = new TreeRule(st.SomeElement, F, acc.Guard, null, lookahead);
                        new_rules.Add(rule);
                    }
                }
            }

            var res1 = new TreeTransducer(roots, inputAlphabet, outputAlphabet, new_states, new_rules);

            if (new_rules.Count == 0)
            {
                res1.clean = true;
                return res1;
            }

            var universalStates = new HashSet<Expr>();
            foreach (var state in new_states)
                if (res1.IsPureAcceptorState(state) && res1.IsUniversal(state))
                    universalStates.Add(state);

            //Case in which initial state is universal
            foreach (var inSt in roots)
                if (universalStates.Contains(inSt))
                {
                    res1.clean = true;
                    return res1;
                }

            TreeTransducer res2 = res1;

            #region remove all universal states from lookaheads
            if (universalStates.Count > 0)
            {
                var new_states2 = new List<Expr>();
                var new_rules2 = new List<TreeRule>();
                foreach (var state in new_states)
                    if (!universalStates.Contains(state))
                        new_states2.Add(state);
                foreach (var rule in new_rules)
                {
                    if (!universalStates.Contains(rule.state))
                    {
                        var lookahaead = new ExprSet[rule.Rank];
                        for (int i = 0; i < lookahaead.Length; i++)
                        {
                            lookahaead[i] = new ExprSet();
                            foreach (var q in rule.Lookahead(i))
                                if (!universalStates.Contains(q))
                                    lookahaead[i].Add(q);
                        }
                        var rule1 = new TreeRule(rule.State, rule.Symbol, rule.Guard, rule.Output, lookahaead);
                        new_rules2.Add(rule1);
                    }
                }
                new_rules = new_rules2;
                new_states = new_states2;
                res2 = new TreeTransducer(roots, inputAlphabet, outputAlphabet, new_states, new_rules);
            }
            #endregion

            var cfg = res2.ToCFG();
            var aliveset = cfg.GetUsefulNonterminals();
            var new_initial_states = new List<Expr>(roots);
            new_initial_states.RemoveAll(state => !aliveset.Contains(state.ToString()));
            new_states.RemoveAll(state => !aliveset.Contains(state.ToString()));
            new_rules.RemoveAll(rule => !rule.IsTrueForAllStates(state => (aliveset.Contains(state.ToString()) || state.Equals(tt.identityState))));


            var res3 = new TreeTransducer(new_initial_states, inputAlphabet, outputAlphabet, new_states, new_rules);

            res3.clean = true;

            //res3.CheckInvariant();

            return res3;
        }

        //bool sinkIsNeeded = false;
        bool uselessRemoved = false;
        /// <summary>
        /// Assumes that this is an acceptor and that all lookaheads are singletons and all guards are satisfiable.
        /// Removes all useless states: in the resulting tree automaton all states are accessible and coaccessible.
        /// </summary>
        public TreeTransducer RemoveUselessStates()
        {
            if (uselessRemoved)
                return this;

            if (!IsAcceptor)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_NotAcceptor);

            var cfg = this.ToCFG();
            var aliveset = cfg.GetUsefulNonterminals();

            var stateSet = new HashSet<string>(aliveset);

            int aliveCount = 0;
            foreach (var s in stateList)
            {
                if (aliveset.Contains(s.ToString()))
                    aliveCount += 1;
            }

            if (stateSet.Count == aliveCount)
            {
                this.uselessRemoved = true;
                return this; //there are no useless states
            }

            var new_initial_states = new List<Expr>(roots);
            new_initial_states.RemoveAll(state => !aliveset.Contains(state.ToString()));
            var new_states = new List<Expr>(this.stateList);
            new_states.RemoveAll(state => !aliveset.Contains(state.ToString()));
            var new_rules = new List<TreeRule>(this.rulesList);
            new_rules.RemoveAll(rule => !rule.IsTrueForAllStates(state => aliveset.Contains(state.ToString())));
            var res = new TreeTransducer(new_initial_states, inputAlphabet, outputAlphabet, new_states, new_rules);

            res.determinized = this.determinized;
            res.uselessRemoved = true;
            //res.sinkIsNeeded = true;

            return res;
        }


        /// <summary>
        /// The result is an automaton with only one initial state
        /// </summary>
        public TreeTransducer RemoveMultipleInitialStates()
        {
            if (roots.Count == 1)
                return this;

            var new_rules = new List<TreeRule>(rulesList);
            var new_states = new List<Expr>(stateList);

            var newState = tt.Z.MkInt(GetMaxStateId() + 1);
            new_states.Add(newState);

            //Add rule from new init state to old init states
            foreach (var rule in rulesList)
                if (roots.Contains(rule.state))
                    new_rules.Add(new TreeRule(newState, rule.Symbol, rule.Guard, rule.Output, rule.lookahead));

            var A = new TreeTransducer(new List<Expr>(new Expr[] { newState }), inputAlphabet, outputAlphabet, new_states, new_rules);
            var A1 = A.Clean();
            return A1;
        }

        internal bool CheckInvariant()
        {
            foreach (var rule in rulesList)
                if (!rule.IsTrueForAllStates(st => ((stateList.Contains(st) && ruleMap.ContainsKey(st) && !st.Equals(tt.identityState)) || st.Equals(tt.identityState))))
                    throw new AutomataException(AutomataExceptionKind.TreeTransducer_InvalidStateId);

            var isAcc = new Dictionary<Expr, bool>();
            foreach (var r in rulesList)
                if (!isAcc.ContainsKey(r.state))
                    isAcc[r.state] = r.IsAcceptorRule;
                else if (isAcc[r.state] != r.IsAcceptorRule)
                    throw new AutomataException(AutomataExceptionKind.TreeTransducer_InvalidStateId);

            return true;
        }

        private IEnumerable<AcceptorBase> EnumerateSimltaneousAcceptors(ConsList<Expr> state_set, AcceptorBase acc)
        {
            if (state_set == null)
                yield return acc;
            else
                foreach (var rule in GetRules(state_set.First, acc.Symbol))
                {
                    var guard = tt.Z.MkAndSimplify(rule.Guard, acc.Guard);
                    if (!guard.Equals(tt.Z.False))
                    {
                        var acc_rule = rule.GetAcceptorRule();
                        var lookahead = new ExprSet[acc.Rank];
                        for (int i = 0; i < lookahead.Length; i++)
                        {
                            lookahead[i] = new ExprSet(acc.Lookahead(i));
                            lookahead[i].AddRange(acc_rule.Lookahead(i));
                        }

                        foreach (var acc1 in EnumerateSimltaneousAcceptors(state_set.Rest, new AcceptorBase(acc.Symbol, guard, lookahead)))
                            yield return acc1;
                    }
                }
        }

        /// <summary>
        /// If the return value is true then q is known to be a universal state
        /// and can be removed from a lookahead.
        /// </summary>
        bool IsUniversal(Expr q)
        {
            var Q = new HashSet<Expr>();
            Q.Add(q);
            return IsUniversal(q, Q);
        }
        bool IsUniversal(Expr q, HashSet<Expr> Q)
        {
            var ruleMap_q = ruleMap[q];
            for (int i = 0; i < inputAlphabet.constructors.Length; i++)
            {
                var F = inputAlphabet.constructors[i];
                List<TreeRule> rules;
                if (!ruleMap_q.TryGetValue(F, out rules))
                    return false;

                List<Expr> guards = new List<Expr>();
                foreach (var rule in rules)
                    guards.Add(rule.Guard);

                if (tt.Z.IsSatisfiable(tt.Z.MkNot(tt.Z.MkOr(guards))))
                    return false; //some attribute is missing
            }
            foreach (var F_rules in ruleMap_q)
                foreach (var rule in F_rules.Value)
                    foreach (var ps in rule.lookahead)
                        foreach (var p in ps)
                            if (Q.Add(p))
                                if (!IsUniversal(p, Q))
                                    return false;

            return true;
        }


        bool determinized = false;

        /// <summary>
        /// Creates an equivalent bottom-up deterministic tree automaton from A.
        /// The returned TA is deterministic and complete where all lookaheads are singletons.
        /// </summary>
        public TreeTransducer Determinize()
        {
            return TreeTransducer.Determinize(this);
        }

        /// <summary>
        /// Creates an equivalent bottom-up deterministic tree automaton from A.
        /// The returned TA is deterministic and complete where all lookaheads are singletons.
        /// </summary>
        static TreeTransducer Determinize(TreeTransducer TA)
        {
            if (TA.determinized)
                return TA;

            if (!TA.IsAcceptor)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_NotAcceptor);

            var A = TA.Complete();

            var tt = A.tt;
            var alph = A.inputAlphabet;

            //all symbols in the rank order
            var symbs = new List<string>[alph.MaxRank + 1];
            for (int i = 0; i < symbs.Length; i++)
                symbs[i] = new List<string>();
            foreach (var s in alph.Symbols)
                symbs[alph.GetRank(s.Name.ToString())].Add(s.Name.ToString());

            Func<Expr, int> Expr2Int = tt.Z.GetNumeralInt;
            Func<int, Expr> Int2Expr = tt.Z.MkInt;

            //bottom-up transitions organized by function symbols
            var delta = new Dictionary<string, Dictionary<Tuple<Sequence<int>, int>, Expr>>();
            foreach (var fnk in alph.Symbols)
                delta[fnk.Name.ToString()] = new Dictionary<Tuple<Sequence<int>, int>, Expr>();

            foreach (var rule in A.rulesList)
            {
                var fdelta = delta[rule.Symbol.Name.ToString()];
                var children = new Sequence<int>(Array.ConvertAll(rule.lookahead, q => Expr2Int(q.SomeElement)));
                var bottomup_move = new Tuple<Sequence<int>, int>(children, Expr2Int(rule.state));
                Expr pred;
                if (fdelta.TryGetValue(bottomup_move, out pred))
                    pred = tt.Z.MkOr(pred, rule.Guard);
                else
                    pred = rule.Guard;
                fdelta[bottomup_move] = pred;
            }

            var stateIds = Array.ConvertAll(A.stateList.ToArray(), tt.Z.GetNumeralInt);

            PowerSetStateBuilder P = PowerSetStateBuilder.Create(stateIds);

            HashSet<int> accStates = new HashSet<int>(Array.ConvertAll(A.roots.ToArray(), tt.Z.GetNumeralInt));

            Func<int, bool> IsAcceptingPowerState = pstate =>
            {
                foreach (int st in P.GetMembers(pstate))
                    if (accStates.Contains(st))
                        return true;

                return false;
            };

            //stack of unprocessed powerstate sequences
            var stack = new Stack<Sequence<int>>();
            stack.Push(Sequence<int>.Empty);

            Func<Sequence<int>, Sequence<int>, bool> IsCrossProductMember = (stateSeq, stateSetSeq) =>
            {
                for (int i = 0; i < stateSeq.Length; i++)
                    if (!P.IsMember(stateSeq[i], stateSetSeq[i]))
                        return false;
                return true;
            };

            var newStates = new HashSet<Expr>();
            var newRules = new List<TreeRule>();
            ConsList<int> newStateIds = null;

            while (stack.Count > 0)
            {
                var qs = stack.Pop();
                var rank = qs.Length;
                foreach (var f in symbs[rank]) //for each symbol of given rank
                {
                    var fnk = alph.GetConstructor(f);
                    var delta_f = delta[f];
                    var fmoves = new List<KeyValuePair<Tuple<Sequence<int>, int>, Expr>>();
                    foreach (var entry in delta_f)
                        if (IsCrossProductMember(entry.Key.Item1, qs))
                            fmoves.Add(entry);

                    var preds = Array.ConvertAll(fmoves.ToArray(), fm => fm.Value);
                    var minterms = tt.Z.GenerateMinterms(preds);

                    foreach (var minterm in minterms)
                    {
                        var pred = tt.Z.ToNNF(tt.Z.Simplify(minterm.Item2));
                        var targetStatesInA = new List<int>();
                        for (int i = 0; i < preds.Length; i++)
                            if (minterm.Item1[i])
                                targetStatesInA.Add(fmoves[i].Key.Item2);
                        if (targetStatesInA.Count == 0)
                            throw new AutomataException(AutomataExceptionKind.InternalError_Determinization);
                        var targetState = P.MakePowerSetState(targetStatesInA);
                        var targetStateExpr = Int2Expr(targetState);
                        var lookahead = Array.ConvertAll(qs.ToArray(), n => new ExprSet(Int2Expr(n)));
                        newRules.Add(new TreeRule(targetStateExpr, fnk, pred, null, lookahead));
                        if (newStates.Add(targetStateExpr))
                        {
                            newStateIds = new ConsList<int>(targetState, newStateIds);

                            for (int k = 1; k <= alph.MaxRank; k++)
                                foreach (var seq in EnumerateX(k, newStateIds))
                                    if (seq.Exists(q => q == targetState)) //make sure targetState occurs at least once
                                        stack.Push(new Sequence<int>(seq.ToArray()));
                        }
                    }
                }
            }

            List<Expr> newAccStates = new List<Expr>();
            List<Expr> newStateList = new List<Expr>(newStates);
            foreach (var q in newStateIds)
            {
                if (IsAcceptingPowerState(q))
                    newAccStates.Add(Int2Expr(q));
            }

            var dta = new TreeTransducer(newAccStates, alph, alph, newStateList, newRules);
            dta.determinized = true;
            dta.complete = true;
            return dta;
        }

        /// <summary>
        /// Creates an equivalent bottom-up deterministic tree automaton from A.
        /// The returned TA is deterministic and all lookaheads are singletons.
        /// </summary>
        public TreeTransducer DeterminizeWithoutCompletion()
        {
            return TreeTransducer.DeterminizeWithoutCompletion(this);
        }

        /// <summary>
        /// Creates an equivalent bottom-up deterministic tree automaton from A.
        /// The returned TA is deterministic and all lookaheads are singletons.
        /// </summary>
        static TreeTransducer DeterminizeWithoutCompletion(TreeTransducer TA)
        {
            if (TA.determinized)
                return TA;

            if (!TA.IsAcceptor)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_NotAcceptor);

            var A = TA;

            var tt = A.tt;
            var alph = A.inputAlphabet;

            //all symbols in the rank order
            var symbs = new List<string>[alph.MaxRank + 1];
            for (int i = 0; i < symbs.Length; i++)
                symbs[i] = new List<string>();
            foreach (var s in alph.Symbols)
                symbs[alph.GetRank(s.Name.ToString())].Add(s.Name.ToString());

            Func<Expr, int> Expr2Int = tt.Z.GetNumeralInt;
            Func<int, Expr> Int2Expr = tt.Z.MkInt;

            //bottom-up transitions organized by function symbols
            var delta = new Dictionary<string, Dictionary<Tuple<Sequence<int>, int>, Expr>>();
            foreach (var fnk in alph.Symbols)
                delta[fnk.Name.ToString()] = new Dictionary<Tuple<Sequence<int>, int>, Expr>();

            foreach (var rule in A.rulesList)
            {
                var fdelta = delta[rule.Symbol.Name.ToString()];
                var children = new Sequence<int>(Array.ConvertAll(rule.lookahead, q => Expr2Int(q.SomeElement)));
                var bottomup_move = new Tuple<Sequence<int>, int>(children, Expr2Int(rule.state));
                Expr pred;
                if (fdelta.TryGetValue(bottomup_move, out pred))
                    pred = tt.Z.MkOr(pred, rule.Guard);
                else
                    pred = rule.Guard;
                fdelta[bottomup_move] = pred;
            }

            var stateIds = Array.ConvertAll(A.stateList.ToArray(), tt.Z.GetNumeralInt);

            PowerSetStateBuilder P = PowerSetStateBuilder.Create(stateIds);

            HashSet<int> accStates = new HashSet<int>(Array.ConvertAll(A.roots.ToArray(), tt.Z.GetNumeralInt));

            Func<int, bool> IsAcceptingPowerState = pstate =>
            {
                foreach (int st in P.GetMembers(pstate))
                    if (accStates.Contains(st))
                        return true;

                return false;
            };

            //stack of unprocessed powerstate sequences
            var stack = new Stack<Sequence<int>>();
            stack.Push(Sequence<int>.Empty);

            Func<Sequence<int>, Sequence<int>, bool> IsCrossProductMember = (stateSeq, stateSetSeq) =>
            {
                for (int i = 0; i < stateSeq.Length; i++)
                    if (!P.IsMember(stateSeq[i], stateSetSeq[i]))
                        return false;
                return true;
            };

            var newStates = new HashSet<Expr>();
            var newRules = new List<TreeRule>();
            ConsList<int> newStateIds = null;

            while (stack.Count > 0)
            {
                var qs = stack.Pop();
                var rank = qs.Length;
                foreach (var f in symbs[rank]) //for each symbol of given rank
                {
                    var fnk = alph.GetConstructor(f);
                    var delta_f = delta[f];
                    var fmoves = new List<KeyValuePair<Tuple<Sequence<int>, int>, Expr>>();
                    foreach (var entry in delta_f)
                        if (IsCrossProductMember(entry.Key.Item1, qs))
                            fmoves.Add(entry);

                    var preds = Array.ConvertAll(fmoves.ToArray(), fm => fm.Value);
                    var minterms = tt.Z.GenerateMinterms(preds);

                    foreach (var minterm in minterms)
                    {
                        var pred = tt.Z.ToNNF(tt.Z.Simplify(minterm.Item2));
                        var targetStatesInA = new List<int>();
                        for (int i = 0; i < preds.Length; i++)
                            if (minterm.Item1[i])
                                targetStatesInA.Add(fmoves[i].Key.Item2);

                        //This lines avoid completion. Only consider rules that go to a target state
                        if (!(targetStatesInA.Count == 0))
                        {
                            var targetState = P.MakePowerSetState(targetStatesInA);
                            var targetStateExpr = Int2Expr(targetState);
                            var lookahead = Array.ConvertAll(qs.ToArray(), n => new ExprSet(Int2Expr(n)));
                            newRules.Add(new TreeRule(targetStateExpr, fnk, pred, null, lookahead));
                            if (newStates.Add(targetStateExpr))
                            {
                                newStateIds = new ConsList<int>(targetState, newStateIds);

                                for (int k = 1; k <= alph.MaxRank; k++)
                                    foreach (var seq in EnumerateMatchRules(k, newStateIds,
                                        new List<TreeRule>(A.GetRulesByRank(k)), P, tt.Z))
                                        if (seq.Exists(q => q == targetState)) //make sure targetState occurs at least once
                                            stack.Push(new Sequence<int>(seq.ToArray()));
                            }
                        }
                    }
                }
            }

            List<Expr> newAccStates = new List<Expr>();
            List<Expr> newStateList = new List<Expr>(newStates);
            foreach (var q in newStateIds)
            {
                if (IsAcceptingPowerState(q))
                    newAccStates.Add(Int2Expr(q));
            }

            var dta = new TreeTransducer(newAccStates, alph, alph, newStateList, newRules);
            dta.determinized = true;
            return dta;
        }

        /// <summary>
        /// Check whether the automaton is bottom-up deterministic
        /// </summary>
        public bool IsDeterminstic()
        {
            if (!clean)
                return false;

            if (determinized)
                return true;

            var A = this;

            var tt = A.tt;
            var alph = A.inputAlphabet;

            //all symbols in the rank order
            var symbs = new List<string>[alph.MaxRank + 1];
            for (int i = 0; i < symbs.Length; i++)
                symbs[i] = new List<string>();
            foreach (var s in alph.Symbols)
                symbs[alph.GetRank(s.Name.ToString())].Add(s.Name.ToString());

            Func<Expr, int> Expr2Int = tt.Z.GetNumeralInt;
            Func<int, Expr> Int2Expr = tt.Z.MkInt;

            //bottom-up transitions organized by function symbols
            var delta = new Dictionary<string, Dictionary<Tuple<Sequence<int>, int>, Expr>>();
            foreach (var fnk in alph.Symbols)
                delta[fnk.Name.ToString()] = new Dictionary<Tuple<Sequence<int>, int>, Expr>();

            foreach (var rule in A.rulesList)
            {
                var fdelta = delta[rule.Symbol.Name.ToString()];
                var children = new Sequence<int>(Array.ConvertAll(rule.lookahead, q => Expr2Int(q.SomeElement)));
                var bottomup_move = new Tuple<Sequence<int>, int>(children, Expr2Int(rule.state));
                Expr pred;
                if (fdelta.TryGetValue(bottomup_move, out pred))
                    pred = tt.Z.MkOr(pred, rule.Guard);
                else
                    pred = rule.Guard;
                fdelta[bottomup_move] = pred;
            }



            foreach (var el in delta)
            {
                var dict = new Dictionary<Sequence<int>, Expr>();

                foreach (var kvp in el.Value)
                {
                    var trig = kvp.Key.Item1;
                    Expr pred;

                    if (dict.TryGetValue(trig, out pred))
                    {
                        if (tt.Z.IsSatisfiable(tt.Z.MkAnd(pred, kvp.Value)))
                            return false;

                        pred = tt.Z.MkOr(pred, kvp.Value);
                    }
                    else
                        pred = kvp.Value;

                    dict[trig] = pred;
                }

            }

            determinized = true;
            return true;
        }

        /// <summary>
        /// Enumerate the tuples of the crossproduct of given length
        /// </summary>
        private static IEnumerable<ConsList<int>> EnumerateX(int length, ConsList<int> elems)
        {
            if (elems == null || length == 0)
                yield return null;
            else
            {
                foreach (var seq in EnumerateX(length - 1, elems))
                    foreach (var q in elems)
                        yield return new ConsList<int>(q, seq);
            }
        }

        /// <summary>
        /// Enumerate the tuples of the crossproduct of given length
        /// </summary>
        private static IEnumerable<ConsList<int>> EnumerateMatchRules(int length, ConsList<int> elems,
            List<TreeRule> rules, PowerSetStateBuilder P, Z3Provider Z)
        {
            if (rules.Count == 0)
                yield break;

            if (elems == null || length == 0)
                yield return null;
            else
            {
                foreach (var q in elems)
                {
                    List<TreeRule> newR = new List<TreeRule>();
                    foreach (var rule in rules)
                    {
                        int index = rule.lookahead.Length - length;
                        var statesinQ = new List<int>(P.GetMembers(q));
                        if (rule.lookahead[index].elements.Exists(r => statesinQ.Contains(
                            Z.GetNumeralInt(r))))
                        {
                            newR.Add(rule);
                        }
                    }
                    if (newR.Count > 0)
                    {
                        foreach (var seq in EnumerateMatchRules(length - 1, elems, newR, P, Z))
                            yield return new ConsList<int>(q, seq);
                    }
                }
            }
        }



        /// <summary>
        /// Minimization based on a symbolic generalization of Moores's algorithm for DFAs.
        /// </summary>
        public TreeTransducer MinimizeOld(bool sink_is_needed = true)
        {
            if (IsEmpty)
                return this;

            var fa = this;
            //var fa = A.RemoveUselessStates(); //fa may be partial here
            //var sink_is_needed = (fa.sinkIsNeeded); //at least one non-coaccessible state was removed

            //make an ordered pair of states
            Func<Expr, Expr, Tuple<Expr, Expr>> MkPair = (x, y) => (TT.Z.GetNumeralInt(x) < TT.Z.GetNumeralInt(y) ? new Tuple<Expr, Expr>(x, y) : new Tuple<Expr, Expr>(y, x));

            var distinguishable = new HashSet<Tuple<Expr, Expr>>();
            var stack = new Stack<Tuple<Expr, Expr>>();

            TreeTransducer fa_sink;
            var sink = fa.tt.Z.MkNumeral(fa.GetMaxStateId() + 1, fa.tt.Z.IntSort);
            //compute partial completion
            if (sink_is_needed)
            {
                #region add sink state and some transitions to the sink state if the sink is needed

                var nu = new Dictionary<Tuple<FuncDecl, Sequence<ExprSet>>, Expr>();
                HashSet<Tuple<FuncDecl, Sequence<ExprSet>>> delta_lhs = null;
                var delta_sink = new HashSet<Tuple<FuncDecl, Sequence<ExprSet>>>();

                foreach (var rule in fa.rulesList)
                {
                    if (nu.ContainsKey(rule.LHS))
                        nu[rule.LHS] = this.tt.Z.MkAnd(nu[rule.LHS], this.tt.Z.MkNot(rule.Guard));
                    else
                        nu[rule.LHS] = this.tt.Z.MkNot(rule.Guard);
                }

                delta_lhs = new HashSet<Tuple<FuncDecl, Sequence<ExprSet>>>(nu.Keys);
                foreach (var key in delta_lhs)
                {
                    if (!this.tt.Z.IsSatisfiable(nu[key]))
                        nu.Remove(key);
                }
                foreach (var key in delta_lhs)
                    for (var i = 0; i < key.Item1.Arity - 1; i++) //Arity is rank + 1
                    {
                        var x = key.Item2[i].SomeElement;
                        foreach (var y in fa.stateList)
                        {
                            if (!x.Equals(y) && (fa.IsRoot(x) == fa.IsRoot(y)))
                            {
                                var z = new Tuple<FuncDecl, Sequence<ExprSet>>(key.Item1, key.Item2.Replace(i, new ExprSet(y)));
                                if (!delta_lhs.Contains(z))
                                    delta_sink.Add(z);
                            }

                        }
                    }
                #endregion

                var new_rules = new List<TreeRule>(fa.rulesList);
                foreach (var maplet in nu)
                    new_rules.Add(new TreeRule(sink, maplet.Key.Item1, maplet.Value, null, maplet.Key.Item2.ToArray()));
                foreach (var elem in delta_sink)
                    new_rules.Add(new TreeRule(sink, elem.Item1, this.tt.Z.True, null, elem.Item2.ToArray()));

                var new_states = new List<Expr>(fa.stateList);
                new_states.Add(sink);

                fa_sink = new TreeTransducer(fa.roots, fa.inputAlphabet, fa.inputAlphabet, new_states, new_rules);
            }
            else
            {
                fa_sink = fa;
            }

            //initialize distinguishability relation
            //all final and nonfinal states are distinguishable 
            //all states are distinguishable from sink
            foreach (var p in fa.stateList)
            {
                if (!fa.IsRoot(p))
                    foreach (var q in fa.roots)
                    {
                        var pair = MkPair(p, q);
                        distinguishable.Add(pair);
                        stack.Push(pair);
                    }
                if (sink_is_needed)
                {
                    var pair = MkPair(p, sink);
                    distinguishable.Add(pair);
                    stack.Push(pair);
                }
            }

            //core of the minimization algorithm
            while (stack.Count > 0)
            {
                var pair = stack.Pop();
                var r = pair.Item1;
                var s = pair.Item2;
                foreach (var rank in fa.inputAlphabet.ranks)
                    if (rank > 0)
                        foreach (var r_rule in fa_sink.GetRulesByRank(r, rank))
                            foreach (var s_rule in fa_sink.GetRulesByRank(s, rank))
                                if (r_rule.Symbol.Equals(s_rule.Symbol))
                                {
                                    int i = r_rule.LHS.Item2.EqAllButOne(s_rule.LHS.Item2);
                                    if (i > -1)
                                    {
                                        var pq = MkPair(r_rule.LHS.Item2[i].SomeElement, s_rule.LHS.Item2[i].SomeElement);
                                        if (!distinguishable.Contains(pq))
                                        {
                                            if (r_rule.Guard.Equals(this.tt.Z.True) || s_rule.Guard.Equals(this.tt.Z.True) ||
                                                this.tt.Z.IsSatisfiable(this.tt.Z.MkAnd(r_rule.Guard, s_rule.Guard)))
                                            {
                                                distinguishable.Add(pq);
                                                stack.Push(pq);
                                            }
                                        }
                                    }
                                }
            }


            var states = fa.stateList.ToArray();

            var repr = new Dictionary<Expr, Expr>();

            for (int i = 0; i < states.Length; i++)
            {
                var p = states[i];
                if (!repr.ContainsKey(p))
                    repr[p] = p;
                for (int j = i + 1; j < states.Length; j++)
                {
                    var q = states[j];
                    if (!distinguishable.Contains(MkPair(p, q)))
                    {
                        repr[q] = repr[p];
                    }
                }
            }

            var guards = new Dictionary<Tuple<Sequence<Expr>, Expr>, Dictionary<FuncDecl, Expr>>();

            foreach (var move in fa.rulesList)
            {
                var p = repr[move.state];
                var newLookahead = new OrderedSet<Expr>[move.Rank];
                List<Expr> outlist = new List<Expr>();
                for (int i = 0; i < move.Rank; i++)
                {
                    var newSt = repr[move.Lookahead(i).SomeElement];
                    outlist.Add(newSt);
                    newLookahead[i] = new OrderedSet<Expr>(newSt);
                }
                var outSeq = new Sequence<Expr>(outlist);
                var pq = new Tuple<Sequence<Expr>, Expr>(outSeq, p);
                if (!guards.ContainsKey(pq))
                    guards[pq] = new Dictionary<FuncDecl, Expr>();

                var g = guards[pq];
                Expr guard;
                if (g.TryGetValue(move.Symbol, out guard))
                    guard = TT.Z.MkOr(guard, move.Guard);
                else
                    guard = move.Guard;
                g[move.Symbol] = guard;
            }

            var moves = new List<TreeRule>();
            foreach (var entry in guards)
            {
                foreach (var symbentry in entry.Value)
                {
                    var larr = entry.Key.Item1.ToArray();
                    var lookahead = new ExprSet[entry.Key.Item1.Length];
                    for (int i = 0; i < lookahead.Length; i++)
                        lookahead[i] = new ExprSet(larr[i]);

                    moves.Add(new TreeRule(entry.Key.Item2, symbentry.Key, symbentry.Value, null, lookahead));
                }
            }

            var finals = new HashSet<Expr>();
            foreach (var final in fa.roots)
                finals.Add(repr[final]);

            var newStates = new HashSet<Expr>();
            foreach (var v in fa.stateList)
                newStates.Add(repr[v]);

            var dta = new TreeTransducer(new List<Expr>(finals), fa.inputAlphabet, fa.outputAlphabet, new List<Expr>(newStates), moves);
            dta.clean = true;
            dta.determinized = true;
            return dta;
        }

        /// <summary>
        /// Minimization based on a symbolic generalization of Moores's algorithm for DFAs.
        /// </summary>
        public TreeTransducer Minimize()
        {
            Func<Expr, int> Expr2int = (x => int.Parse(x.ToString()));
            Func<int, Expr> Int2Expr = tt.Z.MkInt;

            if (IsEmpty)
                return this;

            #region better representation of automaton rules and states
            var C = new Dictionary<int, int>();
            HashSet<int> Q = new HashSet<int>();
            foreach (var st in stateList)
            {
                var sti = Expr2int(st);
                Q.Add(sti);
                C[sti] = 0;
            }
            Q.Add(-1);

            HashSet<int> F = new HashSet<int>();
            foreach (var st in roots)
                F.Add(Expr2int(st));

            var symbolToInt = new Dictionary<string, int>();
            var intToSymbol = new Dictionary<int, FuncDecl>();

            var R = new Dictionary<List<int>, HashSet<Tuple<int, Expr>>>(new ListComparer<int>());
            foreach (var rule in rulesList)
            {
                var symb = rule.Symbol.ToString();
                int symbId;
                if (symbolToInt.ContainsKey(symb))
                    symbId = symbolToInt[symb];
                else
                {
                    symbId = symbolToInt.Count + 1;
                    symbolToInt[symb] = symbId;
                    intToSymbol[symbId] = rule.Symbol;
                }
                var rhs = new List<int>();
                rhs.Add(symbId);
                for (int i = 0; i < rule.Rank; i++)
                {
                    var li = rule.lookahead[i];
                    rhs.Add(Expr2int(li.SomeElement));
                }
                HashSet<Tuple<int, Expr>> value = null;
                if (R.ContainsKey(rhs))
                {
                    value = R[rhs];
                }
                else
                {
                    value = new HashSet<Tuple<int, Expr>>();
                    R[rhs] = value;
                }
                var toState = Expr2int(rule.state);
                value.Add(new Tuple<int, Expr>(toState, rule.Guard));
                C[toState]++;
            }

            #endregion

            var B = new Dictionary<int, HashSet<List<int>>>();
            var P = new Partition(new HashSet<int>(Q));
            var K = new HashSet<int>();
            int numIter = 1;
            foreach (var rule in R)
            {
                var cond = tt.Z.True;
                foreach (var kvpair in rule.Value)
                {
                    HashSet<List<int>> value;
                    var key = kvpair.Item1;
                    cond = tt.Z.MkAnd(cond, kvpair.Item2);
                    if (!B.ContainsKey(key))
                    {
                        value = new HashSet<List<int>>();
                        B[key] = value;
                    }
                    else
                    {
                        value = B[key];
                    }

                    value.Add(rule.Key);
                }
                cond = tt.Z.MkNot(cond);
                if (tt.Z.IsSatisfiable(cond))
                    rule.Value.Add(new Tuple<int, Expr>(-1, cond));
            }

            initial(P, K, Q, F, R);

            while (K.Count > 0 && P.getSize() < Q.Count)
            {
                Dictionary<int, HashSet<int>> subparts = new Dictionary<int, HashSet<int>>();
                var kfirst = 0;
                foreach (var v in K)
                {
                    kfirst = v;
                    break;
                }
                HashSet<int> states = new HashSet<int>(P.block(kfirst));
                ++numIter;

                K.Remove(kfirst);

                foreach (var state in states)
                {
                    foreach (var rightOld in B[state])
                    {
                        foreach (var pairLeftGuard in delta(rightOld, R))
                        {
                            var left1 = pairLeftGuard.Item1;
                            var guard1 = pairLeftGuard.Item2;

                            for (int k = 1; k < rightOld.Count; k++)
                            {
                                //Check this one well
                                var right = new List<int>(rightOld);
                                var q = right[k];

                                var next = P.next(q);
                                right[k] = next;

                                foreach (var pairLeftGuard2 in delta(right, R))
                                {
                                    var left2 = pairLeftGuard2.Item1;
                                    var guard2 = pairLeftGuard2.Item2;
                                    var conj = tt.Z.MkAnd(guard1, guard2);

                                    //in paper left2=delta(...next...) j=left1
                                    if (tt.Z.IsSatisfiable(conj) && !P.equiv(left2, left1))
                                    {
                                        subparts.Clear();

                                        int qrep = P.first(q);
                                        int i = qrep;
                                        var alreadyAdded = new HashSet<int>();

                                        var smallConj = conj;

                                        do
                                        {
                                            var newRight = new List<int>(right);
                                            newRight[k] = i;

                                            foreach (var pairLeftGuard3 in delta(newRight, R))
                                            {
                                                var newConj = tt.Z.MkAnd(smallConj, pairLeftGuard3.Item2);
                                                if (tt.Z.IsSatisfiable(newConj))
                                                {
                                                    var ind = P.first(pairLeftGuard3.Item1);


                                                    HashSet<int> subind;
                                                    if (subparts.ContainsKey(ind))
                                                        subind = subparts[ind];
                                                    else
                                                    {
                                                        subind = new HashSet<int>();
                                                        subparts[ind] = subind;
                                                    }

                                                    subind.Add(i);
                                                    smallConj = newConj;
                                                    break;
                                                }
                                            }
                                            i = P.next(i);

                                        } while (i != P.first(q));


                                        HashSet<int> largest = new HashSet<int>();
                                        foreach (var part in subparts)
                                        {
                                            if (part.Value.Count > largest.Count)
                                                largest = part.Value;
                                        }

                                        foreach (var part in subparts)
                                        {
                                            int j = 0;
                                            foreach (var v in part.Value)
                                            {
                                                j = v;
                                                break;
                                            }
                                            if (!part.Value.Equals(largest))
                                            {
                                                P.refine(part.Value);
                                                K.Add(j);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var res = merge(P, Q, F, C, R);
            F = res.Item1;
            R = res.Item2;

            var newFinStates = new List<int>();
            foreach (var st in F)
            {
                newFinStates.Add(st);
            }
            var newRules = new List<TreeRule>();
            foreach (var rule in R)
            {
                var from = rule.Key;
                string symbol = null;
                var stFrom = new int[from.Count - 1];
                var isFirst = true;
                int ind = 0;
                foreach (var num in from)
                {
                    if (isFirst)
                    {
                        symbol = intToSymbol[num].Name.ToString();
                        isFirst = false;
                    }
                    else
                    {
                        stFrom[ind] = num;
                        ind++;
                    }
                }
                foreach (var kvp in rule.Value)
                {
                    if (kvp.Item1 != -1)
                        newRules.Add(tt.Z.TT.MkTreeAcceptorRule(inputAlphabet, kvp.Item1, symbol, kvp.Item2, stFrom));
                }
            }

            return tt.Z.TT.MkTreeAutomaton(newFinStates.ToArray(), inputAlphabet, outputAlphabet, newRules);
        }

        private Dictionary<int, HashSet<int>> getSubparts(
            int pos,
            int k,
            Expr condition,
            int[] all,
            List<int> right,
            Dictionary<int, HashSet<int>> subparts,
            Partition P,
            Dictionary<List<int>, HashSet<Tuple<int, Expr>>> R)
        {
            if (pos == all.Length)
            {
                return subparts;
            }
            else
            {

                var newRight = new List<int>(right);
                var i = all[pos];
                newRight[k] = i;
                foreach (var pairLeftGuard3 in delta(newRight, R))
                {
                    var newConj = tt.Z.MkAnd(condition, pairLeftGuard3.Item2);
                    if (tt.Z.IsSatisfiable(newConj))
                    {
                        //TODO Should I check some sat again
                        var newSubs = new Dictionary<int, HashSet<int>>(subparts);
                        var ind = P.first(pairLeftGuard3.Item1);
                        HashSet<int> subind;
                        if (newSubs.ContainsKey(ind))
                            subind = newSubs[ind];
                        else
                        {
                            subind = new HashSet<int>();
                            newSubs[ind] = subind;
                        }

                        subind.Add(i);
                        var res = getSubparts(pos + 1, k, newConj, all, right, newSubs, P, R);
                    }
                }
                return null;
            }
        }


        private HashSet<Tuple<int, Expr>> delta(List<int> q, Dictionary<List<int>, HashSet<Tuple<int, Expr>>> R)
        {
            if (R.ContainsKey(q))
                return R[q];

            HashSet<Tuple<int, Expr>> r = new HashSet<Tuple<int, Expr>>();
            r.Add(new Tuple<int, Expr>(-1, TT.Z.True));
            return r;
        }

        private void rmState(int q, HashSet<int> Q, HashSet<int> F, Dictionary<int, int> C)
        {
            Q.Remove(q);
            F.Remove(q);
            C.Remove(q);
        }

        private Tuple<HashSet<int>, Dictionary<List<int>, HashSet<Tuple<int, Expr>>>>
            merge(Partition P, HashSet<int> Q, HashSet<int> F, Dictionary<int, int> C,
            Dictionary<List<int>, HashSet<Tuple<int, Expr>>> R)
        {
            var R1 = new Dictionary<List<int>, HashSet<Tuple<int, Expr>>>();
            var Q1 = new HashSet<int>(Q);
            var F1 = new HashSet<int>(F);
            var C1 = new Dictionary<int, int>(C);
            foreach (var state in Q)
            {
                var rep = P.first(state);
                if (rep != state)
                {
                    C1[rep] += C1[state];
                    rmState(state, Q1, F1, C1);
                }
            }

            ListComparer<int> lc = new ListComparer<int>();
            foreach (var rule in R)
            {
                List<int> r = new List<int>(rule.Key);

                for (int k = 1; k < r.Count; k++)
                    r[k] = P.first(r[k]);

                if (!lc.Equals(r, rule.Key))
                {
                    var set = new HashSet<Tuple<int, Expr>>();
                    foreach (var kvp in rule.Value)
                    {
                        var st = kvp.Item1;
                        var ex = kvp.Item2;
                        set.Add(new Tuple<int, Expr>(P.first(st), ex));
                    }
                    R1[r] = set;
                }
                else
                {
                    var set = new HashSet<Tuple<int, Expr>>();
                    foreach (var kvp in rule.Value)
                    {
                        set.Add(new Tuple<int, Expr>(P.first(kvp.Item1), kvp.Item2));
                    }
                    R1[r] = set;
                }

            }

            //Make sure it changes                        
            return new Tuple<HashSet<int>, Dictionary<List<int>, HashSet<Tuple<int, Expr>>>>(
                F1, R1);
        }

        private void initial(Partition P, HashSet<int> K, HashSet<int> Q, HashSet<int> F,
            Dictionary<List<int>, HashSet<Tuple<int, Expr>>> R)
        {
            Func<Expr, int> Expr2int = (x => int.Parse(x.ToString()));
            Dictionary<int, HashSet<Triple>> signatures =
                new Dictionary<int, HashSet<Triple>>();
            int q = 0;

            foreach (var rule in R)
            {
                var right = rule.Key;

                foreach (var kvp in rule.Value)
                {
                    if (!signatures.ContainsKey(kvp.Item1))
                        signatures[kvp.Item1] = new HashSet<Triple>();

                    for (int k = 1; k < right.Count; ++k)
                    {
                        var t = new Triple(right[0], right.Count - 1, k);
                        q = right[k];
                        if (!signatures.ContainsKey(q))
                            signatures[q] = new HashSet<Triple>();
                        signatures[q].Add(t);
                    }
                }
            }
            foreach (var state in F)
            {
                if (!signatures.ContainsKey(state))
                    signatures[state] = new HashSet<Triple>();
                signatures[state].Add(new Triple(0, 0, 0));
            }
            var blocks = new Dictionary<HashSet<Triple>, HashSet<int>>(new HashSetComparer());
            foreach (var state in Q)
                //TODO this is supposed to be different from sink
                if (state != -1)
                {
                    var ss = signatures[state];
                    HashSet<int> set;
                    if (blocks.ContainsKey(ss))
                    {
                        set = blocks[ss];
                    }
                    else
                    {
                        set = new HashSet<int>();
                        blocks[ss] = set;
                    }
                    set.Add(state);
                }

            foreach (var block in blocks)
            {
                foreach (var v in block.Value)
                {
                    q = v;
                    break;
                }
                P.refine(block.Value);
                K.Add(q);
            }

        }


        /// <summary>
        /// Converts the tree automaton into an quotiented SFA for minimization
        /// </summary>
        public TreeTransducer MinimizeViaSFA()
        {
            if (!this.IsAcceptor)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_NotAcceptor);

            var moves = new List<Move<Expr>>();
            var finals = new HashSet<int>();

            //Initial state that is activated by rank 0 rules
            int q0 = 0;
            int circle = 1;
            Expr circleExpr = tt.Z.MkInt(circle);

            #region Function that extract state number
            var stateIdMap = new Dictionary<Expr, int>();
            int __id__ = 2;
            Func<Expr, int> GetStateId = e =>
            {
                int id;
                if (stateIdMap.TryGetValue(e, out id))
                    return id;
                id = __id__++;
                stateIdMap[e] = id;
                return id;
            };
            #endregion

            #region build datatype sort
            string sortName = "Quotiented";
            int K = this.inputAlphabet.constructors.Length;
            var fieldNames = new string[K][];
            var fieldSorts = new Sort[K][];
            var testerNames = new string[K];
            var constructors = new Constructor[K];
            var mapConstructor = new Dictionary<String, Constructor>();

            for (int i = 0; i < K; i++)
            {
                FuncDecl constructor = inputAlphabet.constructors[i];
                var name = constructor.Name.ToString();
                var arity = constructor.Arity;
                fieldNames[i] = new string[arity];
                fieldSorts[i] = new Sort[arity];
                var field_refs = new uint[arity];
                fieldSorts[i][0] = this.inputAlphabet.AttrSort;
                for (int j = 1; j < arity; j++)
                    fieldSorts[i][j] = TT.Z.IntSort;
                for (int j = 0; j < arity; j++)
                    fieldNames[i][j] = name + "@" + j;

                var c = TT.Z.z3.MkConstructor(name, "$is" + name, fieldNames[i], fieldSorts[i], field_refs);
                constructors[i] = c;
                mapConstructor[name] = c;
            }

            Sort quotientedSort = TT.Z.z3.MkDatatypeSort(sortName, constructors);
            #endregion

            #region Build quotiented SFA
            //Final states are same as for STA
            foreach (var state in this.roots)
                finals.Add(GetStateId(state));

            string dtVarID = "__DT";
            Expr varDT = tt.Z.MkConst(dtVarID, quotientedSort);

            int count = 0;
            // Generate quotiented rules over richer algebra
            foreach (var rule in rulesList)
            {
                count++;
                //We are going bottom-up so the the rule.State is the target
                int target = GetStateId(rule.State);

                var constr = mapConstructor[rule.Symbol.Name.ToString()];
                var f = constr.ConstructorDecl;


                if (rule.Rank == 0)
                {
                    Expr gamma = tt.Z.ApplySubstitution(rule.Guard, inputAlphabet.AttrVar,
                        tt.Z.MkApp(constr.AccessorDecls[0], varDT));
                    gamma = tt.Z.MkAnd(gamma, tt.Z.MkApp(constr.TesterDecl, varDT));
                    moves.Add(Move<Expr>.Create(q0, target, gamma));
                }
                else
                {
                    //rank is >0
                    for (int i = 0; i < rule.Rank; i++)
                    {
                        var lookahead = rule.Lookahead(i);
                        if (!lookahead.IsSingleton)
                            throw new AutomataException(AutomataExceptionKind.TreeTransducer_NotClean);

                        //source state in the SFA transition is the i-th el of the lookahead
                        int qi = GetStateId(lookahead.SomeElement);

                        Expr gamma = tt.Z.ApplySubstitution(rule.Guard, inputAlphabet.AttrVar,
                            tt.Z.MkApp(constr.AccessorDecls[0], varDT));

                        for (int j = 1; j < rule.Rank + 1; j++)
                        {
                            Expr sub = null;
                            if (j == i + 1)
                                sub = tt.Z.MkEq(tt.Z.MkApp(constr.AccessorDecls[j], varDT), circleExpr);
                            else
                            {
                                int id = GetStateId(rule.Lookahead(j - 1).SomeElement);
                                sub = tt.Z.MkEq(tt.Z.MkApp(constr.AccessorDecls[j], varDT), tt.Z.MkInt(id));
                            }

                            gamma = tt.Z.MkAnd(gamma, tt.Z.MkApp(constr.TesterDecl, varDT));
                            gamma = tt.Z.MkAnd(gamma, sub);
                        }

                        moves.Add(Move<Expr>.Create(qi, target, gamma));
                    }
                }
            }

            //Create quotiented SFA
            var autom = Automaton<Expr>.Create(this.tt.Z,q0, finals, moves);
            autom.isDeterministic = true;
            var sfa = new SFA<FuncDecl, Expr, Sort>(this.TT.Z, quotientedSort, autom);
            #endregion

            var b = sfa.Minimize();

            // Get equivalence classes of quotiented SFA and build STA
            Dictionary<int, Block> Blocks = sfa.Automaton.GetStateEquivalenceClasses();
            Dictionary<string, Dictionary<Tuple<List<int>, int>, HashSet<Expr>>> condMap =
                new Dictionary<string, Dictionary<Tuple<List<int>, int>, HashSet<Expr>>>();
            foreach (var move in this.rulesList)
            {
                int to = Blocks[GetStateId(move.state)].Elem();
                int[] from = new int[move.lookahead.Length];
                for (int i = 0; i < from.Length; i++)
                    from[i] = Blocks[GetStateId(move.lookahead[i].SomeElement)].Elem();

                string constr = move.Symbol.Name.ToString();
                Dictionary<Tuple<List<int>, int>, HashSet<Expr>> dict;
                if (!condMap.TryGetValue(constr, out dict))
                {
                    dict = new Dictionary<Tuple<List<int>, int>, HashSet<Expr>>();
                    condMap[constr] = dict;
                }

                var st = new Tuple<List<int>, int>(new List<int>(from), to);
                HashSet<Expr> condSet;
                if (!dict.TryGetValue(st, out condSet))
                {
                    condSet = new HashSet<Expr>();
                    condSet.Add(move.Guard);
                    dict[st] = condSet;
                }
                else
                    condSet.Add(move.Guard);
            }

            var newMoves = new List<TreeRule>();
            var newFinals = new HashSet<int>();
            foreach (var constrDicPair in condMap)
                foreach (var entry in constrDicPair.Value)
                {
                    newMoves.Add(tt.Z.TT.MkTreeAcceptorRule(this.
                        inputAlphabet, entry.Key.Item2, constrDicPair.Key, tt.Z.MkOr(entry.Value), entry.Key.Item1.ToArray()));
                }

            foreach (var f in this.roots)
                newFinals.Add(Blocks[GetStateId(f)].Elem());

            return tt.Z.TT.MkTreeAutomaton(newFinals, inputAlphabet, inputAlphabet, newMoves); ;
        }

        /// <summary>
        /// Create the tree acceptor that accepts the complement of the tree language.
        /// </summary>
        public TreeTransducer Complement()
        {
            var A = Determinize(); //determinization also completes
            var roots_compl = new List<Expr>(A.GetStates());
            roots_compl.RemoveAll(A.IsRoot);
            var Ac = new TreeTransducer(roots_compl, A.inputAlphabet, A.inputAlphabet, A.stateList, A.rulesList);
            Ac.determinized = true;
            Ac.complete = true;
            return Ac;

            //if (newInitStatesSet.Count == 0)
            //    return TreeAutomaton.MkEmpty(A.inputAlphabet);

            //var newInitStates = new List<Expr>(newInitStatesSet);
            //var A2 = new TreeAutomaton(newInitStates, A.inputAlphabet, A.outputAlphabet, A.stateList, A.rulesList);
            //var Ac = A2.Clean();
            //Ac.determinized = true;
            //return Ac;
        }


        internal bool complete = false;
        /// <summary>
        /// Returns true iff the tree automaton is complete as a bottop-up automaton.
        /// </summary>
        bool IsComplete
        {
            get
            {
                return complete;
            }
        }

        /// <summary>
        /// Complete the automaton in the sense of bottom-up automata.
        /// In the completed automaton all lookaheads are singletons.
        /// </summary>
        public TreeTransducer Complete()
        {
            return TreeTransducer.Complete(this);
        }

        /// <summary>
        /// Returns an all-state if there exists one. Returns null otherwise.
        /// An all-state is a state that accepts all input trees.
        /// </summary>
        Expr GetAllState()
        {
            foreach (var q in stateList)
            {
                bool q_is_all_state = true;
                foreach (var s in inputAlphabet.Symbols)
                {
                    if (!ruleMap[q].ContainsKey(s) || ruleMap[q][s].Count != 1)
                    {
                        q_is_all_state = false;
                        break;
                    }
                    TreeRule rule = ruleMap[q][s][0];
                    foreach (ExprSet p in rule.lookahead)
                    {
                        if (!p.IsEmptyOrSingleton || (p.IsSingleton && !p.SomeElement.Equals(q)))
                        {
                            q_is_all_state = false;
                            break;
                        }
                    }
                    if (!q_is_all_state)
                        break;
                }
                if (q_is_all_state)
                    return q;
            }
            return null;
        }

        /// <summary>
        /// Complete the automaton in the sense of bottom-up automata.
        /// In the completed automaton all lookaheads are singletons.
        /// </summary>
        static TreeTransducer Complete(TreeTransducer A)
        {
            if (A.complete)
                return A;

            if (!A.IsAcceptor)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_NotAcceptor);

            A = A.Clean();

            var tt = A.tt;
            var alph = A.inputAlphabet;
            Func<int, Expr> Int2Expr = x => tt.Z.MkNumeral(x, tt.Z.IntSort);

            var qsink = Int2Expr(A.GetMaxStateId() + 1);
            bool qsink_used = false;

            var qall_builtin = A.GetAllState();
            var qall = (qall_builtin != null ? qall_builtin : Int2Expr(A.GetMaxStateId() + 2));
            bool qall_used = false;

            var newrules = new List<TreeRule>();
            var D = new Dictionary<FuncDecl, Dictionary<Sequence<Expr>, Expr>>();
            foreach (var f in alph.Symbols)
            {
                var rank = ((int)f.Arity) - 1;
                var F = new Dictionary<Sequence<Expr>, Expr>();
                D[f] = F;
                foreach (var rule in A.GetRulesForSymbol(f))
                {
                    var rhs = new Expr[rule.Rank];
                    for (int i = 0; i < rule.Rank; i++)
                    {
                        if (rule.Lookahead(i).IsEmpty)
                        {
                            qall_used = true;
                            rhs[i] = qall;
                        }
                        else
                            rhs[i] = rule.Lookahead(i).SomeElement;
                    }
                    var rhsSeq = new Sequence<Expr>(rhs);
                    Expr phi;
                    if (F.TryGetValue(rhsSeq, out phi))
                        phi = tt.Z.MkOr(phi, rule.Guard);
                    else
                        phi = rule.Guard;
                    F[rhsSeq] = phi;
                    newrules.Add(new TreeRule(rule.State, f, rule.Guard, null, Array.ConvertAll(rhs, q => new ExprSet(q))));
                }
                foreach (var entry in F)
                {
                    var compl = tt.Z.MkNot(entry.Value);
                    if (tt.Z.IsSatisfiable(compl))
                    {
                        qsink_used = true;
                        var lookahead = Array.ConvertAll(entry.Key.ToArray(), q => new ExprSet(q));
                        newrules.Add(new TreeRule(qsink, f, compl, null, lookahead));
                    }
                }
            }

            Func<int, int> span = (rank =>
            {
                int m = 1;
                for (int i = 0; i < rank; i++)
                    m = m * A.stateList.Count;
                return m;
            });

            //check if qsink still needs to be added for completion
            if (!qsink_used && qall_used && qall_builtin == null)
            {
                qsink_used = true;
            }
            if (!qsink_used) //see if qsink is still needed
            {
                foreach (var entry in D)
                {
                    var f = entry.Key;
                    var rank = alph.GetRank(f.Name.ToString());
                    var m = span(rank);
                    if (entry.Value.Count < m)
                    {
                        qsink_used = true;
                        break;
                    }
                }
            }

            var newstates = new List<Expr>(A.stateList);

            if (qall_used && qall_builtin == null)
            {
                newstates.Add(qall);
                foreach (var g in alph.Symbols)
                {
                    var g_rank = alph.GetRank(g.Name.ToString());
                    var lookahead = new ExprSet[g_rank];
                    for (int j = 0; j < g_rank; j++)
                        lookahead[j] = new ExprSet(qall);
                    var grule = new TreeRule(qall, g, tt.Z.True, null, lookahead);
                    newrules.Add(grule);
                    var lookaheadSeq = new Sequence<Expr>(Array.ConvertAll(lookahead, es => es.SomeElement));
                    D[g][lookaheadSeq] = tt.Z.True;
                }
            }

            if (qsink_used)
                newstates.Add(qsink);
            if (qall_used && qall_builtin == null)
                newstates.Add(qall);

            if (qsink_used)
            {
                foreach (var f in alph.Symbols)
                    if (f.Arity > 1 || (f.Arity == 1 && D[f].Count == 0))
                    {
                        var rank = ((int)f.Arity) - 1;
                        foreach (var qs in GenerateStateSequences(rank, newstates))
                        {
                            var qseq = (qs == null ? Sequence<Expr>.Empty : new Sequence<Expr>(qs));
                            if (!D[f].ContainsKey(qseq)) //in case f has rank 0 it must be an unused symbol
                            {
                                var lookahead = new ExprSet[rank];
                                for (int i = 0; i < rank; i++)
                                    lookahead[i] = new ExprSet(qseq[i]);
                                var rule = new TreeRule(qsink, f, tt.Z.True, null, lookahead);
                                newrules.Add(rule);
                            }
                        }
                    }
            }

            if (!(qsink_used || qall_used))
            {
                A.complete = true;
                return A;
            }

            var res = new TreeTransducer(A.roots, alph, alph, newstates, newrules);
            res.complete = true;
            return res;

            #region old algo
            //var symbs = new List<string>[alph.MaxRank + 1];
            //for (int i = 0; i < symbs.Length; i++)
            //    symbs[i] = new List<string>();
            //foreach (var s in alph.Symbols)
            //    symbs[alph.GetRank(s.Name.ToString())].Add(s.Name.ToString());

            //Func<Expr, int> Expr2Int = tt.Z.GetNumeralInt;


            //var delta = new Dictionary<string, Dictionary<Sequence<int>, Dictionary<Expr, int>>>();
            //foreach (var fnk in alph.Symbols)
            //    delta[fnk.Name.ToString()] = new Dictionary<Sequence<int>, Dictionary<Expr, int>>();

            //bool qsinkIsUsed = false;
            //foreach (var rule in A.rulesList)
            //{
            //    var fdelta = delta[rule.symbol.Name.ToString()];
            //    var state = Expr2Int(rule.state);
            //    var childlist = new List<int>();
            //    foreach (var ts in rule.lookahead)
            //    {
            //        if (ts.IsEmpty)
            //        {
            //            qsinkIsUsed = true;
            //            childlist.Add(qsink);
            //        }
            //        else
            //            childlist.Add(Expr2Int(ts.SomeElement));
            //    }
            //    var children = new Sequence<int>(childlist.ToArray());
            //    Dictionary<Expr, int> pred_state;
            //    if (!fdelta.TryGetValue(children, out pred_state))
            //    {
            //        pred_state = new Dictionary<Expr, int>();
            //        fdelta[children] = pred_state;
            //    }
            //    pred_state[rule.guard] = Expr2Int(rule.state);
            //}

            //var stateIds = new List<int>(Array.ConvertAll(A.stateList.ToArray(), tt.Z.GetNumeralInt));
            //if (qsinkIsUsed)
            //{
            //    stateIds.Add(qsink);
            //    foreach (var fnk in alph.Symbols)
            //    {
            //        var fname = fnk.Name.ToString();
            //        var rank = alph.GetRank(fname);
            //        var children = new int[rank];
            //        for (int i = 0; i < rank; i++)
            //            children[i] = qsink;
            //        var childrenSeq = new Sequence<int>(children);
            //        var pred_state_dict = new Dictionary<Expr, int>();
            //        pred_state_dict[tt.Z.True] = qsink;
            //        delta[fname][childrenSeq] = pred_state_dict;
            //    }
            //}

            //var qbot = (qsinkIsUsed ? qsink + 1 : qsink);

            //#region actual completion
            ////complete wrt predicates
            //foreach (var fnk in alph.Symbols)
            //{
            //    var fdelta = delta[fnk.Name.ToString()];
            //    var complement = new Dictionary<Sequence<int>, Expr>();
            //    foreach (var entry in fdelta)
            //        complement[entry.Key] = tt.Z.MkNot(tt.Z.MkOr(entry.Value.Keys));
            //    foreach (var entry in complement)
            //        if (tt.Z.IsSatisfiable(entry.Value))
            //        {
            //            fdelta[entry.Key][tt.Z.Simplify(entry.Value)] = qbot;
            //        }
            //}
            //stateIds.Add(qbot);
            //var stateIdList = ConsList<int>.Create(stateIds);

            ////complete wrt combinations of states
            //var botcase = new Dictionary<Expr, int>(1);
            //botcase[tt.Z.True] = qbot;
            //for (int rank = 0; rank < symbs.Length; rank++)
            //    foreach (var seq in EnumerateX(rank, stateIdList))
            //    {
            //        var s = (seq == null ? Sequence<int>.Empty : new Sequence<int>(seq.ToArray()));
            //        foreach (var f in symbs[rank])
            //            if (!delta[f].ContainsKey(s))
            //                delta[f][s] = botcase;
            //    }
            //#endregion

            //    List<Expr> newStates = new List<Expr>(Array.ConvertAll(stateIds.ToArray(), n => Int2Expr(n)));
            //List<TreeRule> newRules = new List<TreeRule>();
            //foreach (var fnk in alph.Symbols)
            //{
            //    var f = fnk.Name.ToString();
            //    var fdelta = delta[f];
            //    foreach (var entry in fdelta)
            //    {
            //        var lookahead = Array.ConvertAll(entry.Key.ToArray(), q => new ExprSet(Int2Expr(q)));
            //        foreach (var pred_state in entry.Value)
            //            newRules.Add(new TreeRule(Int2Expr(pred_state.Value), fnk, pred_state.Key, null, lookahead));
            //    }
            //}

            //var res = new TreeAutomaton(A.initialStates, alph, alph, newStates, newRules);
            //res.complete = true;
            //if (A.determinized)
            //    res.determinized = true;
            //return res;

            #endregion
        }

        static private IEnumerable<ConsList<Expr>> GenerateStateSequences(int k, IEnumerable<Expr> states)
        {
            if (k == 0)
                yield return null;
            else
                foreach (var rest in GenerateStateSequences(k - 1, states))
                    foreach (var q in states)
                        yield return new ConsList<Expr>(q, rest);
        }

        Dictionary<FuncDecl, List<TreeRule>> rulesForSymbol = new Dictionary<FuncDecl, List<TreeRule>>();
        private List<TreeRule> GetRulesForSymbol(FuncDecl f)
        {
            return rulesForSymbol[f];
        }

        //static List<Sequence<int>> GetCrossProductOfMembers(PowerSetStateBuilder P, Sequence<int> setseq)
        //{
        //    var res = new List<Sequence<int>>();
        //    if (setseq.Length == 0)
        //    {
        //        res.Add(Sequence<int>.Empty);
        //    }
        //    else
        //    {
        //        var inp = ConsList<int>.Create(setseq.ToArray());
        //        foreach (var outp in EnumerateCrossProductOfMembers(P, inp))
        //            res.Add(new Sequence<int>(outp.ToArray()));
        //    }
        //    return res;
        //}

        //static IEnumerable<ConsList<int>> EnumerateCrossProductOfMembers(PowerSetStateBuilder P, ConsList<int> setseq)
        //{
        //    if (setseq == null)
        //        yield return null;
        //    else
        //        foreach (var first in P.GetMembers(setseq.First))
        //            foreach (var rest in EnumerateCrossProductOfMembers(P, setseq))
        //                yield return new ConsList<int>(first, rest);
        //}


        /// <summary>
        /// Converts the tree automaton into an equivalent SFA.
        /// The attribute of the symbol with rank 0 is ignored.
        /// </summary>
        public SFA<FuncDecl, Expr, Sort> ConvertToSFA()
        {
            if (!this.IsAcceptor)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_NotAcceptor);

            if (!this.inputAlphabet.IsSFAcompatible)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_NotSFAcompatible);

            var moves = new List<Move<Expr>>();
            var finals = new HashSet<int>();
            int __sink__ = 0;
            var __init__ = 1;
            int __id__ = 2;
            var stateIdMap = new Dictionary<Expr, int>();
            stateIdMap[Root] = __init__;
            Func<Expr, int> GetStateId = e =>
            {
                int id;
                if (stateIdMap.TryGetValue(e, out id))
                    return id;
                id = __id__++;
                stateIdMap[e] = id;
                return id;
            };
            foreach (var rule in rulesList)
            {
                int source = GetStateId(rule.State);
                int target = __sink__;
                if (rule.Rank == 1)
                {
                    var lookahead = rule.Lookahead(0);
                    if (!lookahead.IsEmptyOrSingleton)
                        throw new AutomataException(AutomataExceptionKind.TreeTransducer_NotClean);
                    if (lookahead.IsEmpty & finals.Add(__sink__))
                        moves.Add(Move<Expr>.Create(__sink__, __sink__, this.TT.Z.True));
                    else
                        target = GetStateId(lookahead.SomeElement);
                    moves.Add(Move<Expr>.Create(source, target, rule.Guard));
                }
                else //rank is 0, treat the state as final, ignore the attribute
                {
                    finals.Add(source);
                }
            }

            var autom = Automaton<Expr>.Create(this.tt.Z,__init__, finals, moves);
            var sfa = new SFA<FuncDecl, Expr, Sort>(this.TT.Z, this.inputAlphabet.AttrSort, autom);
            return sfa;
        }

        /// <summary>
        /// Converts an SFA into an equivalent tree automaton.
        /// </summary>
        /// <param name="sfa">given SFA whose input sort must be the attribute sort of the alphabet alph</param>
        /// <param name="alph">alphabet that must contain one rank-0 and one rank-1 symbol and no other symbols</param>
        public static TreeTransducer ConvertFromSFA(SFA<FuncDecl, Expr, Sort> sfa, RankedAlphabet alph)
        {
            sfa = sfa.EliminateEpsilons();
            if (!alph.IsSFAcompatible)
                throw new AutomataException(AutomataExceptionKind.RankedAlphabet_IsNotSFAcompatible);

            if (!sfa.InputSort.Equals(alph.AttrSort))
                throw new AutomataException(AutomataExceptionKind.SortMismatch);

            if (!sfa.Solver.Equals(alph.TT.Z))
                throw new AutomataException(AutomataExceptionKind.SolversAreNotIdentical);


            List<TreeRule> rules = new List<TreeRule>();

            var nil = alph.SFA_Nil;
            var cons = alph.SFA_Cons;

            foreach (var move in sfa.GetMoves())
            {
                var rule = new TreeRule(alph.TT.Z.MkInt(move.SourceState), cons, move.Label, null, new ExprSet(alph.TT.Z.MkInt(move.TargetState)));
                rules.Add(rule);
            }
            foreach (var st in sfa.Automaton.GetFinalStates())
            {
                var rule = new TreeRule(alph.TT.Z.MkInt(st), nil, alph.TT.Z.True, null);
                rules.Add(rule);
            }

            var ta = alph.TT.MkTreeAutomaton(sfa.InitialState, alph, alph, rules);
            return ta;
        }
    }

    /*
    /// <summary>
    /// Used in Compose and Apply methods above.
    /// </summary>
    internal class TransductionComposer
    {
        Func<Expr, Expr, Expr> composeStates;
        Func<Expr, FuncDecl, IEnumerable<TreeRule>> getRules;
        Z3Provider Z;
        TreeTheory tt;

        internal TransductionComposer(TreeTheory tt,
            Func<Expr, Expr, Expr> stateComposer,
            Func<Expr, FuncDecl, IEnumerable<TreeRule>> ruleProvider)
        {
            this.tt = tt;
            this.Z = tt.Z;
            this.composeStates = stateComposer;
            this.getRules = ruleProvider;
        }

        /// <summary>
        /// Reduce the mixed term to irreducible guarded terms.
        /// The first element is the context (path condition), 
        /// the second element is an irreducible output term that does not contain nested applications of $trans.
        /// </summary>
        /// <param name="mixedExpr">term that may include nested applications of $trans</param>
        /// <param name="context">condition on attributes</param>
        internal IEnumerable<Tuple<Expr, Expr>> Reduce(Expr context, Expr mixedExpr)
        {
            if (mixedExpr == null || IsNotMixed(mixedExpr))
            {
                yield return new Tuple<Expr, Expr>(Z.True, mixedExpr);
            }
            else
            {
                Sort C = mixedExpr.Sort;
                FuncDecl f = mixedExpr.FuncDecl;
                var f_args = mixedExpr.Args;
                if (TreeTheory.IsTrans(f))
                {
                    #region f is $trans_B_C

                    var p = f_args[0];       //state
                    Expr gterm = f_args[1];  //input tree

                    if (gterm.ASTKind == Z3_ast_kind.Z3_VAR_AST)
                    {
                        //should never be the case because transductions are normalized
                        throw new AutomataException(AutomataExceptionKind.UnexpectedExprKind);

                        ////input tree is a child that is output directly
                        //var q = tt.identityState;
                        //Expr input_subtree = gterm;
                        ////again, the first arg is the source state and the second is the source tree 
                        ////e.g. gterm = $trans_A_B(q, input_subtree) 
                        ////i.e. mixedExpr = $trans_B_C(p, $trans_A_B(q, input_subtree)) 
                        ////where f = $trans_B_C, g = $trans_A_B,
                        //Expr qp = composeStates(q, p);
                        ////get the free transforrmer h = $trans_A_C
                        //var h = z3p.TT.GetTrans(qp.Sort, input_subtree.Sort, z3p.GetRange(f));
                        //var res = z3p.MkApp(h, qp, input_subtree);
                        //yield return new Tuple<Expr, Expr>(z3p.True, res);
                    }
                    else
                    {
                        if (gterm.ASTKind != Z3_ast_kind.Z3_APP_AST)
                            throw new AutomataException(AutomataExceptionKind.UnexpectedExprKind);

                        FuncDecl g = gterm.FuncDecl;
                        if (TreeTheory.IsTrans(g))
                        {
                            //g is $trans_A_B
                            var g_args = gterm.Args;
                            var q = g_args[0];
                            Expr input_subtree = g_args[1];
                            //again, the first arg is the source state and the second is the source tree 
                            //e.g. gterm = $trans_A_B(q, input_subtree) 
                            //i.e. mixedExpr = $trans_B_C(p, $trans_A_B(q, input_subtree)) 
                            //where f = $trans_B_C, g = $trans_A_B,
                            Expr qp = composeStates(q, p);
                            //get the free transformer h = $trans_A_C
                            var trans = tt.GetTrans(input_subtree.Sort, Z.GetRange(f));
                            var res = Z.MkApp(trans, qp, input_subtree);
                            yield return new Tuple<Expr, Expr>(Z.True, res);
                        }
                        else if (tt.GetRankedAlphabet(gterm.Sort).ContainsConstructor(g))
                        {
                            foreach (var pair in ApplyTransformationRules(context, p, gterm))
                                yield return pair;
                        }
                        else
                        {
                            //g must either be $trans or a constructor
                            throw new AutomataException(AutomataExceptionKind.UnexpectedFunctionSymbol);
                        }
                    }

                    #endregion
                }
                else if (tt.GetRankedAlphabet(C).ContainsConstructor(f))
                {
                    #region f is a constructor
                    //descend the tree structure and reduce recursively
                    var argsList = ConsList<Expr>.Create(f_args);
                    var nodeLabel = argsList.First; //we know that the arity is at least 1
                    var subtrees = argsList.Rest;
                    if (subtrees == null) //should not be the case here, because it should not contain transducers
                    {
                        throw new AutomataException(AutomataExceptionKind.InternalError);
                    }
                    foreach (var reduced_subtrees in ReduceSubtrees(context, subtrees))
                    {
                        var args1 = reduced_subtrees.ToArray();
                        var guards1 = Array.ConvertAll(args1, x => x.First);
                        var terms1 = Array.ConvertAll(args1, x => x.Second);
                        var terms01 = new Expr[terms1.Length + 1];
                        terms01[0] = nodeLabel;
                        Array.Copy(terms1, 0, terms01, 1, terms1.Length);
                        var term1 = Z.MkApp(f, terms01);
                        var guard = Z.MkAnd(guards1);
                        var context_guard = Z.MkAndSimplify(context, guard);
                        if (!context_guard.Equals(Z.False))
                            yield return new Tuple<Expr, Expr>(guard, term1);
                    }
                    #endregion
                }
                else
                {
                    //f must be either a transformer or a constructor
                    throw new AutomataException(AutomataExceptionKind.UnexpectedFunctionSymbol);
                }
            }
        }

        private bool IsNotMixed(Expr maybeMixedExpr)
        {
            if (maybeMixedExpr.ASTKind != Z3_ast_kind.Z3_APP_AST)
                return true;
            else if (TreeTheory.IsTrans(maybeMixedExpr.FuncDecl))
                return false;
            else
                return Array.TrueForAll(maybeMixedExpr.Args, t => IsNotMixed(t));
        }

        private IEnumerable<ConsList<Tuple<Expr, Expr>>> ReduceSubtrees(Expr context, ConsList<Expr> terms)
        {
            if (terms == null)
                yield return null;
            else
                foreach (var first in Reduce(context, terms.First))
                    foreach (var rest in ReduceSubtrees(context, terms.Rest))
                        yield return new ConsList<Tuple<Expr, Expr>>(first, rest);
        }

        internal Expr MkAnd(Expr context, Expr guard)
        {
            //if identical
            if (context.Equals(guard))
                return context;

            //if context implies the guard 
            //then just return the context
            if (context.Equals(Z.True))
                return guard;

            if (guard.Equals(Z.True) || !Z.IsSatisfiable(Z.MkAnd(context, Z.MkNot(guard))))
                return context;

            if (!Z.IsSatisfiable(Z.MkAnd(guard, Z.MkNot(context))))
                return guard;

            if (!Z.IsSatisfiable(Z.MkAnd(guard, context)))
                return Z.False;

            return Z.Z3.MkAnd(context, guard);
        }

        private IEnumerable<Tuple<Expr, Expr>> ApplyTransformationRules(Expr context, Expr p, Expr t)
        {
            var alph = tt.GetRankedAlphabet(t.Sort);
            FuncDecl f = t.FuncDecl;  //f is known to be a constructor 
            Expr[] args = t.Args; //each constructor has arity at least 1, the first argument is the attribute
            Expr[] vars = new Expr[args.Length];
            vars[0] = alph.AttrVar;
            for (int i = 1; i < vars.Length; i++)
                vars[i] = alph.ChildVar(i);
            var rules = getRules(p, f);
            foreach (var rule in rules)
            {
                var guard = Z.ApplySubstitution(rule.Guard, vars[0], args[0]);
                var context_and_guard = Z.MkAndSimplify(context, guard);
                if (!Z.False.Equals(context_and_guard))
                {
                    var t1 = (rule.IsAcceptorRule ? null : Z.ApplySubstitution(rule.Output, vars, args));
                    foreach (var t2 in Reduce(context_and_guard, t1))
                        yield return new Tuple<Expr, Expr>(MkAnd(guard, t2.First), t2.Second);
                }
            }
        }
    }



    */

    /// <summary>
    /// Used in Compose and Apply methods above.
    /// </summary>
    internal class Reducer
    {
        Func<Expr, Expr, Expr> MkState;
        Z3Provider Z;
        TreeTheory tt;
        TreeTransducer B;

        internal Reducer(TreeTransducer B, Func<Expr, Expr, Expr> MkComposedState)
        {
            this.B = B;
            this.tt = B.TT;
            this.Z = B.TT.Z;
            this.MkState = MkComposedState;
        }


        Dictionary<ReductionValue, HashSet<ReductionValue>> reduce_lookup = new Dictionary<ReductionValue, HashSet<ReductionValue>>();

        internal IEnumerable<ReductionValue> Reduce(ReductionValue rv)
        {
            HashSet<ReductionValue> reduced;
            if (reduce_lookup.TryGetValue(rv, out reduced))
            {
                foreach (var v in reduced)
                    yield return v;
            }
            else
            {
                var set = new HashSet<ReductionValue>();
                reduce_lookup[rv] = set;
                foreach (var v in Reduce1(rv))
                    if (set.Add(v))
                        yield return v;
            }
        }

        internal IEnumerable<ReductionValue> Reduce1(ReductionValue rv)
        {
            if (rv.output == null)
            {
                //output null corresponds to an acceptor rule
                //so there are no output reductions to be made, i.e., null reduces to null
                yield return rv;
            }
            else
            {
                FuncDecl f = rv.output.FuncDecl;
                var f_args = rv.output.Args;

                if (TreeTheory.IsTrans(f))
                {
                    #region f is $trans_B

                    var p = f_args[0];       //state
                    Expr gterm = f_args[1];  //input tree

                    if (gterm.ASTKind != Z3_ast_kind.Z3_APP_AST)
                        throw new AutomataException(AutomataExceptionKind.UnexpectedExprKind);

                    FuncDecl g = gterm.FuncDecl;
                    if (TreeTheory.IsTrans(g))  //$trans(p, $trans(q, y))
                    {
                        #region g is $trans_A
                        var g_args = gterm.Args;
                        var q = g_args[0];
                        Expr y = g_args[1];
                        if ((Z.GetVarIndex(y) < 1 || (Z.GetVarIndex(y)) > rv.given.Length))
                            throw new AutomataException(AutomataExceptionKind.TreeTransducer_UnexpectedOutputExpr);

                        Expr qp = MkState(q, p);
                        var trans = tt.GetTrans(y.Sort, Z.GetRange(f));
                        var res = Z.MkApp(trans, qp, y);
                        if (!qp.Equals(tt.identityState))
                            rv.given[Z.GetVarIndex(y) - 1].Add(qp);
                        yield return new ReductionValue(rv.guard, res, rv.given);
                        #endregion
                    }
                    else if (tt.GetRankedAlphabet(gterm.Sort).ContainsConstructor(g))
                    {
                        #region g is a constructor of the alphabet of A
                        foreach (var rv1 in ApplyTransformationRules(rv.guard, rv.given, p, gterm))
                            yield return rv1;
                        #endregion
                    }
                    else
                    {
                        throw new AutomataException(AutomataExceptionKind.UnexpectedFunctionSymbol);
                    }

                    #endregion
                }
                else if (B.OutputAlphabet.ContainsConstructor(f))
                {
                    #region f is a constructor
                    //descend the tree structure and reduce recursively
                    var argsList = ConsList<Expr>.Create(f_args);
                    var nodeLabel = argsList.First; //we know that the arity is at least 1, the first element is the attribute
                    var subtrees = argsList.Rest;
                    foreach (var rv_list in ReduceSubtrees(rv.guard, rv.given, subtrees))
                    {
                        Expr t1 = Z.MkApp(f, new ConsList<Expr>(argsList.First, rv_list.outputs).ToArray());
                        var rv1 = new ReductionValue(rv_list.guard, t1, rv_list.given);
                        yield return rv1;
                    }
                    #endregion
                }
                else
                {
                    #region error case
                    //f must be either a transformer or a constructor, any other case is an error
                    throw new AutomataException(AutomataExceptionKind.UnexpectedFunctionSymbol);
                    #endregion
                }
            }
        }

        /// <summary>
        /// Apply all possible transformations from state p to output t in the given context.
        /// rv = (context, given, $tran(p,t))
        /// </summary>
        internal IEnumerable<ReductionValue> ApplyTransformationRules(Expr context, ExprSet[] given, Expr p, Expr t)
        {
            var alph = tt.GetRankedAlphabet(t.Sort);

            FuncDecl f = t.FuncDecl;  //f is known to be a constructor 
            Expr[] f_args = t.Args; //each constructor has arity at least 1, the first argument is the attribute
            Expr[] vars = new Expr[f_args.Length];
            vars[0] = alph.AttrVar;
            for (int i = 1; i < vars.Length; i++)
                vars[i] = alph.ChildVar(i);

            var pf_rules = B.GetRules(p, f);

            foreach (var pf_rule in pf_rules)
            {
                var guard = Z.Simplify(Z.ApplySubstitution(pf_rule.Guard, vars[0], f_args[0]));
                if (Z.IsSatisfiable(guard))
                {
                    var attrCond1 = tt.Z.MkAndSimplify(context, guard);
                    if (!attrCond1.Equals(Z.False))
                    {
                        //compute composed lookahead formula for the child subtrees    
                        var args1 = new Expr[f_args.Length - 1];
                        Array.Copy(f_args, 1, args1, 0, args1.Length);
                        var childLookaheadCond = alph.MkLangConj(pf_rule.lookahead, args1);


                        var output1 = (pf_rule.IsAcceptorRule ? null : Z.Simplify(Z.ApplySubstitution(pf_rule.Output, vars, f_args)));

                        //consider all possible reductions of the lookahead condition;
                        foreach (var rv1 in ReduceLanguageCondition(childLookaheadCond, new ReductionValue(attrCond1, output1, given)))
                            foreach (var rv2 in Reduce(rv1))
                                yield return rv2;
                    }
                }
            }
        }

        /// <summary>
        /// The condition is a conjunction of language state applications.
        /// </summary>
        private IEnumerable<ReductionValue> ReduceLanguageCondition(Expr condition, ReductionValue v)
        {
            if (condition.Equals(Z.True))
                yield return v;
            else
            {
                var f = condition.FuncDecl;
                var args = condition.Args;
                var fkind = f.DeclKind;
                if (fkind == Z3_decl_kind.Z3_OP_AND)
                {
                    if (args.Length != 2)  //only binary uses of And can occur
                        throw new AutomataException(AutomataExceptionKind.InternalError);

                    foreach (var v1 in ReduceLanguageCondition(args[0], v))
                        foreach (var v2 in ReduceLanguageCondition(args[1], v1))
                            yield return v2;
                }
                else if (fkind == Z3_decl_kind.Z3_OP_UNINTERPRETED)
                {
                    if (TreeTheory.IsLang(f))
                    {
                        if (args.Length != 2)
                            throw new AutomataException(AutomataExceptionKind.InternalError);

                        var q = args[0];
                        var s = args[1];
                        foreach (var v1 in ReduceLanguageApp(q, s, v))
                            yield return v1;
                    }
                }
                else
                {
                    throw new AutomataException(AutomataExceptionKind.InternalError);
                }
            }
        }

        /// <summary>
        /// Special case language state application $lang(q, s)
        /// </summary>
        private IEnumerable<ReductionValue> ReduceLanguageApp(Expr q, Expr s, ReductionValue v)
        {
            if (s.ASTKind != Z3_ast_kind.Z3_APP_AST)
                throw new AutomataException(AutomataExceptionKind.InternalError);

            var g = s.FuncDecl;
            var args = s.Args;

            var gkind = g.ASTKind;
            if (TreeTheory.IsTrans(g))
            {
                #region direct application $lang(q, $trans(p, y)) where p is a transducer state and y a child variable
                var p = args[0];
                var y = args[1];

                if (!Z.IsVar(y))
                    throw new AutomataException(AutomataExceptionKind.InternalError);

                int i = (int)Z.GetVarIndex(y);

                if (i < 1 || i > v.given.Length)
                    throw new AutomataException(AutomataExceptionKind.InternalError);

                //add the pair (p,q) to the i'th child language condition
                var pq = MkState(p, q); //new OrderedPair<Expr, Expr>(p, q);
                var lang = v.given[i - 1].Union(pq);
                var langs = new ExprSet[v.given.Length];
                for (int j = 0; j < langs.Length; j++)
                    langs[j] = (j == (i - 1) ? lang : v.given[j]);

                var v1 = new ReductionValue(v.guard, v.output, langs);
                yield return v1;
                #endregion
            }
            else
            {
                #region application $lang(q, g(t0, t1, .., tk)) where g is a constructor of arity k
                var gsort = tt.Z.GetRange(g);
                RankedAlphabet galph = tt.GetRankedAlphabet(gsort);
                if (!galph.ContainsConstructor(g))
                    throw new AutomataException(AutomataExceptionKind.InternalError);

                //the language rules from state q, the output part of the rules is not relevant in this case
                var rules = B.GetRules(q, g);

                foreach (var rule in rules)
                {
                    var guard = Z.Simplify(Z.ApplySubstitution(rule.Guard, Z.MkVar(0, args[0].Sort), args[0]));
                    if (Z.IsSatisfiable(guard))
                    {
                        var guard1 = Z.MkAndSimplify(v.guard, guard);
                        if (!guard1.Equals(Z.False))
                        {
                            var args1 = new Expr[args.Length - 1];
                            Array.Copy(args, 1, args1, 0, args1.Length);
                            var s1 = galph.MkLangConj(rule.lookahead, args1);
                            var v1 = new ReductionValue(guard1, v.output, v.given);
                            foreach (var v2 in ReduceLanguageCondition(s1, v1))
                                yield return v2;
                        }
                    }
                }
                #endregion
            }
        }

        private bool IsNotMixed(Expr maybeMixedExpr)
        {
            if (maybeMixedExpr.ASTKind != Z3_ast_kind.Z3_APP_AST)
                return true;
            else if (TreeTheory.IsTrans(maybeMixedExpr.FuncDecl))
                return false;
            else
                return Array.TrueForAll(maybeMixedExpr.Args, t => IsNotMixed(t));
        }

        /// <summary>
        /// If terms = [t1,t2,t3] then the result is Reduce(t1) x Reduce(t2) x Reduce(t3)
        /// </summary>
        private IEnumerable<ReductionValueList> ReduceSubtrees(Expr context, ExprSet[] given, ConsList<Expr> terms)
        {
            if (terms == null)
                yield return new ReductionValueList(context, null, given);
            else
                foreach (var first in Reduce(new ReductionValue(context, terms.First, given)))
                    foreach (var rest in ReduceSubtrees(first.guard, first.given, terms.Rest))
                        yield return new ReductionValueList(rest.guard, new ConsList<Expr>(first.output, rest.outputs), rest.given);
        }

        private class ReductionValueList
        {
            internal Expr guard;
            internal ExprSet[] given;
            internal ConsList<Expr> outputs;

            internal ReductionValueList(Expr guard, ConsList<Expr> outputs, ExprSet[] given)
            {
                this.guard = guard;
                this.outputs = outputs;
                this.given = given;
            }
        }
    }

    internal class ReductionValue
    {
        internal Expr guard;
        internal ExprSet[] given;
        internal Expr output; //may be null

        internal ReductionValue(Expr guard, Expr output, int rank)
        {
            this.guard = guard;
            this.output = output;
            this.given = new ExprSet[rank];
            for (int i = 0; i < rank; i++)
                this.given[i] = new ExprSet();
        }

        internal ReductionValue(Expr guard, Expr output, params ExprSet[] given)
        {
            this.guard = guard;
            this.output = output;
            this.given = given;
        }

        public override bool Equals(object obj)
        {
            ReductionValue rv = obj as ReductionValue;
            if (rv == null)
                return false;
            if (!rv.guard.Equals(guard))
                return false;
            if (rv.given.Length != given.Length)
                return false;
            if (rv.output == null && output != null)
                return false;
            if (rv.output != null && output == null)
                return false;
            if (output != null && !output.Equals(rv.output))
                return false;
            for (int i = 0; i < given.Length; i++)
                if (!given[i].Equals(rv.given[i]))
                    return false;
            return true;
        }

        public override int GetHashCode()
        {
            int hv = (output == null ? guard.GetHashCode() : output.GetHashCode() + (guard.GetHashCode() << 1));
            for (int i = 0; i < given.Length; i++)
                hv += (given[i].GetHashCode() << (2 + i));
            return hv;
        }
    }
}

