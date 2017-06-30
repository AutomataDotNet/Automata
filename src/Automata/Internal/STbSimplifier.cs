using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Microsoft.Automata
{

    internal enum Bool3 { Yes, No, Unknown };
    internal class STbSimplifier<FUNC, TERM, SORT>
    {
        STb<FUNC, TERM, SORT> stb;

        ST<FUNC, TERM, SORT> st;

        IPFbuilder<TERM> pfb;

        internal ST<FUNC, TERM, SORT> ST_without_yields { get { return st; } }

        IContext<FUNC, TERM, SORT> solver;

        internal STbSimplifier(STb<FUNC, TERM, SORT> stb)
        {
            this.stb = stb;
            this.st = stb.ToST(true);
            this.solver = stb.Solver;
            this.pfb = new PFbuilder1<FUNC, TERM, SORT>(stb.Solver, stb.InputVar, stb.RegVar);
        }

        bool ContainsRegVar(TERM t)
        {
            foreach (var v in solver.GetVars(t))
                if (v.Equals(stb.RegVar))
                    return true;
            return false;
        }

        bool ContainsInpVar(TERM t)
        {
            foreach (var v in solver.GetVars(t))
                if (v.Equals(stb.InputVar))
                    return true;
            return false;
        }

        /// <summary>
        /// Returns true if lhs implies rhs for all variables
        /// </summary>
        bool Implies(TERM lhs, TERM rhs)
        {
            var psi = solver.MkAnd(lhs, solver.MkNot(rhs));
            var res = !solver.IsSatisfiable(psi);
            return res;
        }


        public Bool3 IsReachable(int state, TERM condition, int searchdepth = -1)
        {
            var x = solver.MkVar(0, stb.InputListSort);
            var y = stb.RegVar;

            var x_is_cons = solver.MkIsCons(x);
            var first_of_x = solver.MkFirstOfList(x);
            var rest_of_x = solver.MkRestOfList(x);

            var predicate = condition;
            if (ContainsInpVar(condition))
                predicate = solver.MkAnd(x_is_cons,
                    solver.ApplySubstitution(condition, stb.InputVar, first_of_x));


            var next_layer = new SimpleStack<int>();
            var next_Psi = new Dictionary<int, TERM>();
            var curr_Psi = new Dictionary<int, TERM>();
            var curr_layer = new SimpleStack<int>();
            var Sigma = new Dictionary<int, TERM>(); //accumulated reachability condition
            next_Psi[state] = predicate;
            Sigma[state] = predicate;



            next_layer.Push(state);

            Action<int, TERM> UpdateSearch = (q, psi) =>
                {
                    if (solver.IsSatisfiable(psi))
                    {
                        TERM gamma_q;
                        if (Sigma.TryGetValue(q, out gamma_q))
                        {
                            //check if Forall y x(pred(x,y) => Psi_q(x,y))
                            //observe that x is intended to be existentially quantified
                            //so this check is a sufficient condition to check 
                            //that Forall y(Exists x(pred(x,y)) => Exists x(Psi_q(x,y)))
                            //but in general it might not detect the implication
                            //and may cause redundant search
                            if (!psi.Equals(gamma_q)) //otherwise implication is trivial
                                if (!Implies(psi, gamma_q))
                                {
                                    if (next_Psi.ContainsKey(q))
                                        next_Psi[q] = solver.MkOr(next_Psi[q], psi);
                                    else
                                    {
                                        next_Psi[q] = psi;
                                        next_layer.Push(q);
                                    }
                                    Sigma[q] = solver.MkOr(gamma_q, psi);
                                }
                        }
                        else
                        {
                            next_layer.Push(q);
                            next_Psi[q] = psi;
                            Sigma[q] = psi;
                        }
                    }
                };

            int k = 0;
            while (next_layer.IsNonempty)
            {
                var tmp_curr_layer = curr_layer;
                curr_layer = next_layer;
                var tmp_curr_Psi = curr_Psi;
                curr_Psi = next_Psi;
                next_Psi = tmp_curr_Psi;
                next_Psi.Clear();
                next_layer = tmp_curr_layer;
                while (curr_layer.IsNonempty)
                {
                    var q = curr_layer.Pop();
                    var psi = curr_Psi[q];

                    if (q == stb.InitialState)
                    {
                        var psi1 = solver.ApplySubstitution(psi, y, stb.InitialRegister);
                        if (solver.IsSatisfiable(psi1))
                            return Bool3.Yes;
                    }

                    foreach (var move in st.automaton.GetMovesTo(q))
                    {
                        if (!move.Label.IsFinal)
                        {
                            TERM phi_source;
                            if (!ContainsRegVar(move.Label.Guard) && !ContainsInpVar(move.Label.Update))
                            {
                                phi_source = solver.ApplySubstitution(psi, y, move.Label.Update);
                            }
                            else
                            {
                                var update_first = solver.ApplySubstitution(move.Label.Update, stb.InputVar, first_of_x);
                                var psi_rest = solver.ApplySubstitution(psi, y, update_first, x, rest_of_x);
                                var guard_first = solver.ApplySubstitution(move.Label.Guard, stb.InputVar, first_of_x);
                                phi_source = solver.MkAnd(x_is_cons, guard_first, psi_rest);
                            }
                            UpdateSearch(move.SourceState, phi_source);
                        }
                    }
                }
                if (searchdepth >= 0)
                {
                    //terminate if searchdepth == k
                    if (k == searchdepth)
                        break;
                }
                k += 1;
            }
            if (next_layer.IsEmpty)
                return Bool3.No;
            else
                return Bool3.Unknown;
        }


        //public STb<FUNC, TERM, SORT> Simplify()
        //{
        //    //check first if anything other than the initial register value is possible in the initial state

        //    int K = 2 * stb.StateCount;

        //    bool onlyInitialRegisterValueIsReacahbleAtInitialState =
        //        (IsReachable(stb.InitialState, solver.MkNot(solver.MkEq(stb.RegVar, stb.InitialRegister)), K) == Bool3.No);

        //    var initPF = pfb.Create(stb.Solver.True, stb.InitialRegister);


        //    var state = stb.InitialState;

        //    var next_layer = new SimpleStack<int>();
        //    var next_Psi = new Dictionary<int, IPF<TERM>>();
        //    var curr_Psi = new Dictionary<int, IPF<TERM>>();
        //    var curr_layer = new SimpleStack<int>();
        //    var Psi = new Dictionary<int, IPF<TERM>>(); //accumulated reachability condition
        //    Psi[stb.InitialState] = initPF;
        //    next_Psi[stb.InitialState] = initPF;

        //    next_layer.Push(state);

        //    Action<int, IPF<TERM>> UpdateSearch = (q, pred) =>
        //    {
        //        if (pred.IsSatisfiable)
        //        {
        //            IPF<TERM> Psi_q;
        //            if (Psi.TryGetValue(q, out Psi_q))
        //            {
        //                if (!pred.Equals(Psi_q)) //otherwise implication is trivial
        //                {
        //                    var pred1 = pred;
        //                    //var pred1 = pred.RemoveCasesSubsumedBy(Psi_q);
        //                    if (pred1.IsSatisfiable)
        //                    {
        //                        if (next_Psi.ContainsKey(q))
        //                            next_Psi[q] = next_Psi[q].Merge(pred1);
        //                        else
        //                        {
        //                            next_Psi[q] = pred1;
        //                            next_layer.Push(q);
        //                        }
        //                        Psi[q] = Psi_q.Merge(pred1);
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                next_layer.Push(q);
        //                next_Psi[q] = pred;
        //                Psi[q] = pred;
        //            }
        //        }
        //    };

        //    int k = 0;
        //    while (next_layer.IsNonempty)
        //    {
        //        var tmp_curr_layer = curr_layer;
        //        curr_layer = next_layer;
        //        var tmp_curr_Psi = curr_Psi;
        //        curr_Psi = next_Psi;
        //        next_Psi = tmp_curr_Psi;
        //        next_Psi.Clear();
        //        next_layer = tmp_curr_layer;
        //        while (curr_layer.IsNonempty)
        //        {
        //            var q = curr_layer.Pop();
        //            var phi = curr_Psi[q];
        //            foreach (var move in st.GetNonFinalMovesFrom(q))
        //                //if initreg is the only possible value in the initial state then do not revisit it
        //                if (!(onlyInitialRegisterValueIsReacahbleAtInitialState && move.TargetState == stb.InitialState))
        //                {
        //                    var phi1 = phi.Extend(move.Label.Guard, move.Label.Update);
        //                    UpdateSearch(move.TargetState, phi1);
        //                }
        //        }
        //        if (k == K)
        //            break;
        //        k += 1;
        //    }

        //    //var trans = new List<Move<Rule<TERM>>>();
        //    //foreach (var move in st.GetMoves())
        //    //    if (!move.Label.IsFinal)
        //    //    {
        //    //        var psi = Psi[move.SourceState];
        //    //        var psi1 = psi.Extend(move.Label.Guard, move.Label.Update);
        //    //        if (psi1.IsEmpty)
        //    //            trans.Add(move);
        //    //    }

        //    var res = new STb<FUNC, TERM, SORT>(stb.Solver, st.Name + "S", stb.InputSort, stb.OutputSort, stb.RegisterSort, stb.InitialRegister, stb.InitialState);

        //    var done = new HashSet<int>();
        //    var stack = new SimpleStack<int>();
        //    stack.Push(stb.InitialState);
        //    done.Add(stb.InitialState);

        //    Action<int> updateSearch = q =>
        //        {
        //            if (done.Add(q))
        //                stack.Push(q);
        //        };

        //    Action<int> noop = q => { };

        //    while (stack.IsNonempty)
        //    {
        //        var q = stack.Pop();

        //        var rule1 = (Psi.ContainsKey(q) ? Prune(Psi[q], stb.GetRuleFrom(q), updateSearch) : UndefRule<TERM>.Default);
        //        res.AssignRule(q, rule1);
        //        var frule = stb.GetFinalRuleFrom(q);
        //        if (frule.IsNotUndef)
        //        {
        //            var frule1 = (Psi.ContainsKey(q) ? Prune(Psi[q], frule, noop) : UndefRule<TERM>.Default);
        //            if (!(frule1 is UndefRule<TERM>))
        //                res.AssignFinalRule(q, frule1);
        //        }
        //    }
        //    var res_st = res.ToST(true);

        //    if (res_st.StateCount < res.StateCount)
        //    {
        //        var res1 = new STb<FUNC, TERM, SORT>(stb.Solver, st.Name + "S", stb.InputSort, stb.OutputSort, stb.RegisterSort, stb.InitialRegister, stb.InitialState);
        //        foreach (var q in res_st.GetStates())
        //        {
        //            var rule_q = res.GetRuleFrom(q).Prune(res_st.automaton.IsState);
        //            res1.AssignRule(q, rule_q);
        //            var frule_q = res.GetFinalRuleFrom(q);
        //            if (frule_q.IsNotUndef)
        //                res1.AssignFinalRule(q, frule_q);
        //        }
        //        var res2 = res1.ExploreBools();
        //        return res2;
        //    }
        //    else
        //    {
        //        var res1 = res.ExploreBools(); //eliminates unused registers
        //        return res1;
        //    }
        //}


        public bool ContainsVar(TERM term, TERM variable)
        {
            foreach (var v in solver.GetVars(term))
                if (v.Equals(variable))
                    return true;
            return false;
        }

        Dictionary<TERM, bool> IsSat_Cache = new Dictionary<TERM, bool>();

        bool IsSat(TERM pred)
        {
            bool res;
            if (IsSat_Cache.TryGetValue(pred, out res))
                return res;
            else
            {
                res = solver.IsSatisfiable(pred);
                IsSat_Cache[pred] = res;
                return res;
            }
        }


        public STb<FUNC, TERM, SORT> Simplify(int searchBound = -1)
        {
            var exp = new STbSimulator<FUNC, TERM, SORT>(stb);
            exp.Explore();
            HashSet<BaseRule<TERM>> unreachable_baserules = new HashSet<BaseRule<TERM>>();
            HashSet<BaseRule<TERM>> unreachable_final_baserules = new HashSet<BaseRule<TERM>>();
            //for each of the uncovered cases see if the condition is unreachable
            //List<Tuple<ConsList<bool>, BaseRule<TERM>, Move<Rule<TERM>>>> unreachable =
            //    new List<Tuple<ConsList<bool>, BaseRule<TERM>, Move<Rule<TERM>>>>();
            //List<Tuple<ConsList<bool>, BaseRule<TERM>, Move<Rule<TERM>>>> maybe_reachable =
            //    new List<Tuple<ConsList<bool>, BaseRule<TERM>, Move<Rule<TERM>>>>();
            //List<Tuple<ConsList<bool>, BaseRule<TERM>, Move<Rule<TERM>>>> reachable =
            //    new List<Tuple<ConsList<bool>, BaseRule<TERM>, Move<Rule<TERM>>>>();
            var k = (searchBound < 0 ? (stb.StateCount == 1 ? 2 : stb.StateCount) : searchBound);
            var x_list_Var = solver.MkVar(0, solver.MkListSort(stb.InputSort));
            foreach (var elem in exp.UncoveredMoves)
            {
                var check = IsReachable(elem.Item3.SourceState, elem.Item3.Label.Guard, k);
                if (check == Bool3.No)
                    unreachable_baserules.Add(elem.Item2);
            }
            foreach (var elem in exp.UncoveredFinalMoves)
            {
                var check = IsReachable(elem.Item3.SourceState, elem.Item3.Label.Guard, k);
                if (check == Bool3.No)
                    unreachable_final_baserules.Add(elem.Item2);
            }
            var name = string.Format("Simpl{0}_{1}", (searchBound >= 0 ? searchBound.ToString() : ""), stb.Name);
            var stb1 = new STb<FUNC, TERM, SORT>(solver, name, stb.InputSort, stb.OutputSort, stb.RegisterSort, stb.InitialRegister, stb.InitialState);
            foreach (var q in stb.States)
            {
                var rule_q = stb.GetRuleFrom(q).Prune(unreachable_baserules.Contains);
                stb1.AssignRule(q, rule_q);
                var frule_q = stb.GetFinalRuleFrom(q).Prune(unreachable_final_baserules.Contains);
                stb1.AssignFinalRule(q, frule_q);
            }
            stb1.EliminateDeadends();
            var stbRes = stb1.ExploreBools();
            return stbRes;
        }
        STb<FUNC, TERM, SORT> Simplify_(int bound, bool checkInitialState)
        {
            Func<Rule<TERM>, bool> IsEpsilon = (r) =>
                {
                    if (r.IsFinal)
                        return true;
                    else if (ContainsVar(r.Guard, stb.RegVar))
                        return false;
                    else if (IsSat(solver.MkAnd(r.Guard, solver.MkNeq(r.Update, stb.RegVar))))
                        return false;
                    else
                        return true;
                };

            Func<Rule<TERM>, Rule<TERM>> ReplaceEps = (r) =>
                {
                    if (IsEpsilon(r))
                        return null;
                    else
                        return r;
                };

            var aut = st.automaton.RelpaceAllGuards(ReplaceEps);
            var q0 = aut.InitialState;

            bool onlyInitialRegisterValueIsReacahbleAtInitialState = false;

            if (checkInitialState)
                onlyInitialRegisterValueIsReacahbleAtInitialState =
                   (IsReachable(stb.InitialState, solver.MkNot(solver.MkEq(stb.RegVar, stb.InitialRegister)), st.StateCount + 1) == Bool3.No);

            var Pf = new Dictionary<int, Tuple<bool, IPF<TERM>>>();

            var isNotFixed = new HashSet<int>(aut.States);
            var isfixed = new HashSet<int>();
            var impossibleMoves = new HashSet<Move<Rule<TERM>>>();
            var inaccessibleStates = new HashSet<int>();

            if (onlyInitialRegisterValueIsReacahbleAtInitialState)
            {
                Pf[q0] = new Tuple<bool, IPF<TERM>>(false, pfb.Create(solver.True, stb.InitialRegister));
                isfixed.Add(q0);
                isNotFixed.Remove(q0);
                foreach (var move in aut.GetMovesFrom(q0))
                {
                    //we know that aut has at most one move to any target state
                    if (move.TargetState != q0 && aut.GetMovesCountTo(move.TargetState) == 1)
                    {
                        isfixed.Add(move.TargetState);
                        isNotFixed.Remove(move.TargetState);
                        //propagate the initial register
                        var guard1 = solver.ApplySubstitution(move.Label.guard, stb.RegVar, stb.InitialRegister);
                        var update1 = solver.ApplySubstitution(move.Label.update, stb.RegVar, stb.InitialRegister);
                        var pf = pfb.Create(guard1, update1);
                        Pf[move.TargetState] = new Tuple<bool, IPF<TERM>>(false, pf);
                    }
                }
                bool isfixedChanged = false;
                if (isfixed.Count > 1)
                    isfixedChanged = true;

                while (isfixedChanged)
                {
                    isfixedChanged = false;
                    foreach (var q in isNotFixed)
                    {
                        var visinity = new List<Move<Rule<TERM>>>(aut.GetNonepsilonMovesTo(aut.GetInvEpsilonClosure(q))).ToArray();
                        if (Array.TrueForAll(visinity, m => isfixed.Contains(m.SourceState)))
                        {
                            isfixedChanged = true; 
                            var paths = new List<IPF<TERM>>();
                            foreach (var m in visinity)
                            {
                                var pf = Pf[m.SourceState].Item2.Extend(m.Label.Guard, m.Label.Update);
                                if (!pf.IsSatisfiable)
                                    impossibleMoves.Add(m);
                                else
                                    paths.Add(pf);
                            }
                            isfixed.Add(q);
                            Pf[q] = new Tuple<bool, IPF<TERM>>(false, pfb.MergeAll(paths));
                            if (paths.Count == 0)
                                inaccessibleStates.Add(q);
                        }
                    }
                    isNotFixed.RemoveWhere(isfixed.Contains);
                }
            }

            foreach (var m in impossibleMoves)
                aut.RemoveTheMove(m);


            foreach (int q in isNotFixed)
                Pf[q] = new Tuple<bool, IPF<TERM>>(true, pfb.True); //initially all possible values are possible

            bool somePsiEntryChanged = true;
            int k = 0;

            //keep strengthening the conditions until a search bound is reached or until no more progress is made
            while (somePsiEntryChanged)
            {
                somePsiEntryChanged = false;
                var Pf_next = new Dictionary<int, Tuple<bool, IPF<TERM>>>();
                foreach (int q in aut.States)
                {
                    if (isfixed.Contains(q))
                        Pf_next[q] = Pf[q];

                    else if (!Pf[q].Item2.IsSatisfiable)
                        Pf_next[q] = new Tuple<bool, IPF<TERM>>(false, pfb.False);

                    else
                    {
                        var pfs_in = new List<Tuple<Rule<TERM>, Tuple<bool, IPF<TERM>>>>();
                        foreach (var move in aut.GetNonepsilonMovesTo(aut.GetInvEpsilonClosure(q)))
                            if (Pf[move.SourceState].Item2.IsSatisfiable) //else the source state is unreachable
                                pfs_in.Add(new Tuple<Rule<TERM>, Tuple<bool, IPF<TERM>>>(move.Label, Pf[move.SourceState]));
                        if (Array.Exists(pfs_in.ToArray(), entry => entry.Item2.Item1 == true))
                        {
                            IPF<TERM> pf = null;
                            foreach (var elem in pfs_in)
                                if (pf == null)
                                    pf = elem.Item2.Item2.Extend(elem.Item1.Guard, elem.Item1.Update);
                                else
                                    pf = pf.Merge(elem.Item2.Item2.Extend(elem.Item1.Guard, elem.Item1.Update));
                            Pf_next[q] = new Tuple<bool, IPF<TERM>>(true, pf);
                        }
                        else
                        {
                            Pf_next[q] = new Tuple<bool, IPF<TERM>>(false, Pf[q].Item2);
                        }
                    }
                }
                foreach (int q in aut.States)
                {
                    if (!(Pf_next[q].Item1))
                        Pf[q] = Pf_next[q];
                    else if (Pf[q].Item2.Implies(Pf_next[q].Item2))
                        Pf[q] = new Tuple<bool, IPF<TERM>>(false, Pf[q].Item2);
                    else
                    {
                        Pf[q] = Pf_next[q];
                        somePsiEntryChanged = true;
                    }
                }
                k = k + 1;
                if (bound >= 0 && k > bound)
                    break;
            }
            var name = string.Format("Simpl_{0}_{1}({2})", (bound >= 0 ? bound.ToString() : ""), k, st.Name);
            var res = new STb<FUNC, TERM, SORT>(stb.Solver, name, stb.InputSort, stb.OutputSort, stb.RegisterSort, stb.InitialRegister, stb.InitialState);

            var stack = new SimpleStack<int>();
            var done = new HashSet<int>();
            stack.Push(stb.InitialState);
            done.Add(stb.InitialState);

            Action<int> updateSearch = q =>
            {
                if (done.Add(q))
                    stack.Push(q);
            };

            Action<int> noop = q => { };

            while (stack.IsNonempty)
            {
                var q = stack.Pop();
                var rule1 = (Pf[q].Item2.IsSatisfiable ? Prune(Pf[q].Item2, stb.GetRuleFrom(q), updateSearch) :  UndefRule<TERM>.Default);
                res.AssignRule(q, rule1);
                var frule = stb.GetFinalRuleFrom(q);
                if (frule.IsNotUndef)
                {
                    var frule1 = (Pf[q].Item2.IsSatisfiable ? Prune(Pf[q].Item2, frule, noop) : UndefRule<TERM>.Default);
                    res.AssignFinalRule(q, frule1);
                }
            }
            var res_st = res.ToST(true);

            Predicate<BaseRule<TERM>> IsNotValidState = (r) =>
            {
                return !res_st.automaton.IsState(r.State);
            };

            if (res_st.StateCount < res.StateCount)
            {
                var res1 = new STb<FUNC, TERM, SORT>(stb.Solver, name, stb.InputSort, stb.OutputSort, stb.RegisterSort, stb.InitialRegister, stb.InitialState);
                foreach (var q in res_st.GetStates())
                {
                    var rule_q = res.GetRuleFrom(q).Prune(IsNotValidState);
                    res1.AssignRule(q, rule_q);
                    var frule_q = res.GetFinalRuleFrom(q);
                    if (frule_q.IsNotUndef)
                        res1.AssignFinalRule(q, frule_q);
                }
                var res2 = res1.ExploreBools();
                return res2;
            }
            else
            {
                var res1 = res.ExploreBools(); //eliminates unused registers
                return res1;
            }
        }

        BranchingRule<TERM> Prune(IPF<TERM> pf, BranchingRule<TERM> rule, Action<int> updateSearch)
        {
            return PruneHelp(pf, stb.Solver.True, rule, updateSearch);
        }
        BranchingRule<TERM> PruneHelp(IPF<TERM> pf, TERM cond, BranchingRule<TERM> rule, Action<int> updateSearch)
        {
            if (rule is SwitchRule<TERM>)
                throw new NotImplementedException("Prune(SwitchRule)");

            switch (rule.RuleKind)
            {
                case BranchingRuleKind.Base:
                    updateSearch(rule.State);
                    return rule;
                case BranchingRuleKind.Ite:
                    {
                        var cond_t = stb.Solver.MkAnd(cond, rule.Condition);
                        var cond_f = stb.Solver.MkAnd(cond, stb.Solver.MkNot(rule.Condition));
                        var pf_t = pf.Extend(cond_t, stb.RegVar);
                        if (!pf_t.IsSatisfiable)
                            return PruneHelp(pf, cond_f, rule.FalseCase, updateSearch);
                        else
                        {
                            var pf_f = pf.Extend(cond_f, stb.RegVar);
                            if (!pf_f.IsSatisfiable)
                                return PruneHelp(pf, cond_t, rule.TrueCase, updateSearch);
                            else
                            {
                                var t = PruneHelp(pf, cond_t, rule.TrueCase, updateSearch);
                                var f = PruneHelp(pf, cond_f, rule.FalseCase, updateSearch);
                                if ((t is UndefRule<TERM>) && (f is UndefRule<TERM>))
                                    return t;
                                else
                                    return new IteRule<TERM>(rule.Condition, t, f);
                            }
                        }
                    }
                case BranchingRuleKind.Undef:
                    return UndefRule<TERM>.Default;
                default:
                    throw new NotImplementedException(BranchingRuleKind.Switch.ToString());
            }
        }
    }

    internal interface IPF<TERM>
    {
        bool IsSatisfiable { get; }
        IPF<TERM> Extend(TERM guard, TERM update);
        IPF<TERM> Merge(IPF<TERM> summary);
        TERM Summary { get; }
        bool Implies(IPF<TERM> rhs);
    }

    internal abstract class IPFbuilder<TERM>
    {
        public abstract IPF<TERM> Create(TERM guard, TERM update);
        public abstract TERM RegVar { get; }
        public abstract IPF<TERM> True { get; }
        public abstract IPF<TERM> False { get; }

        public IPF<TERM> MergeAll(IEnumerable<IPF<TERM>> paths)
        {
            IPF<TERM> res = null;
            foreach (var pf in paths)
                res = (res == null ? pf : res.Merge(pf));
            if (res == null)
                return False;
            else
                return res;
        }
    }

    internal class PFbuilder1<FUNC, TERM, SORT> : IPFbuilder<TERM>
    {
        internal IContext<FUNC, TERM, SORT> context;
        TERM _y;
        TERM _x;
        FUNC X;
        TERM x_list;
        TERM rest_of_x_list;
        TERM first_of_x_list;
        TERM is_cons_x_list;
        TERM y0;
        IPF<TERM> tt;

        override public IPF<TERM> True
        {
            get { return tt; }
        }

        override public IPF<TERM> False
        {
            get { return Empty; }
        }

        IPF<TERM> Empty;

        override public TERM RegVar
        {
            get { return _y; }
        }

        public PFbuilder1(IContext<FUNC, TERM, SORT> context, TERM inpVar, TERM regVar)
        {
            this.context = context;
            this._x = inpVar;
            this._y = regVar;
            this.X = context.MkFreshFuncDecl("X", new SORT[] { context.IntSort }, context.GetSort(inpVar));
            this.Empty = new PF1(this);
            this.x_list = context.MkVar(0, context.MkListSort(context.GetSort(inpVar)));
            this.rest_of_x_list = context.MkRestOfList(x_list);
            this.first_of_x_list = context.MkFirstOfList(x_list);
            this.is_cons_x_list = context.MkIsCons(x_list);
            this.y0 = context.MkFreshConst("y0", context.GetSort(regVar));
            this.tt = new PF1(this, context.True, _y);
        }

        bool ContainsVar(TERM t, TERM v)
        {
            foreach (var w in context.GetVars(t))
                if (w.Equals(v))
                    return true;
            return false;
        }

        override public IPF<TERM> Create(TERM guard, TERM update)
        {
            return new PF1(this, guard, update);
        }

        class PF1 : IPF<TERM>
        {
            PFbuilder1<FUNC, TERM, SORT> pfb;
            Sequence<PFcase> cases;

            internal IContext<FUNC, TERM, SORT> context { get { return pfb.context; } }
            TERM x { get { return pfb._x; } }
            TERM y { get { return pfb._y; } }
            TERM x_list { get { return pfb.x_list; } }
            TERM first_of_x_list { get { return pfb.first_of_x_list; } }


            /// <summary>
            /// Returns true if this(inputs,register_in,register_out) implies pf(inputs,register_in,register_out)
            /// </summary>
            public bool Implies(IPF<TERM> pf)
            {
                if (this.cases.IsEmpty)
                    return true;

                var pred = context.MkAnd(this.Summary, context.MkNot(pf.Summary));
                bool implies = !context.IsSatisfiable(pred);
                return implies;
            }

            public bool IsSatisfiable { get { return !cases.IsEmpty; } }

            IEnumerable<TERM> EnumerateCaseSummaries()
            {
                foreach (var f in cases)
                    yield return f.GetSummary();
            }

            TERM summary = default(TERM);
            public TERM Summary
            {
                get
                {
                    if (object.Equals(summary, default(TERM)))
                        summary = context.MkOr(EnumerateCaseSummaries());
                    return summary;
                }
            }

            public override bool Equals(object obj)
            {
                var pf = obj as IPF<TERM>;
                if (pf == null)
                    return false;
                return (pf.Summary.Equals(this.Summary));
            }

            public override int GetHashCode()
            {
                return Summary.GetHashCode();
            }

            public override string ToString()
            {
                return cases.ToString();
            }

            public PF1(PFbuilder1<FUNC, TERM, SORT> pfb)
            {
                this.pfb = pfb;
                this.cases = Sequence<PFcase>.Empty;
            }

            public PF1(PFbuilder1<FUNC, TERM, SORT> pfb, TERM guard, TERM update)
            {
                this.pfb = pfb;
                this.cases = new Sequence<PFcase>(new PFcase(pfb, guard, update));
            }

            PF1(PFcase pfc)
            {
                this.pfb = pfc.pfb;
                this.cases = new Sequence<PFcase>(pfc);
            }

            public IPF<TERM> Merge(IPF<TERM> ipf)
            {
                var pf = (PF1)ipf;
                if (this.cases.Length == 1 && pf.cases.Length == 1)
                {
                    var cond = context.MkAnd(this.cases[0].guard, pf.cases[0].guard, context.MkNeq(this.cases[0].update, pf.cases[0].update));
                    if (!context.IsSatisfiable(cond))
                    {
                        var same = context.MkAnd(this.cases[0].guard, pf.cases[0].guard);
                        var ite = context.MkIte(same, this.cases[0].update, context.MkIte(this.cases[0].guard, this.cases[0].update, pf.cases[0].update));
                        var pfc = MkPFcaseDirectly(context.MkOr(this.cases[0].guard, pf.cases[0].guard), ite, pf.cases[0].k);
                        return new PF1(pfc);
                    }
                }
                var merged = new PF1(pf.pfb);
                merged.cases = this.cases;
                foreach (var f in pf.cases)
                    //if (f.IsSubsumedBy(merged.cases) != Bool3.Yes)
                        merged.cases = merged.cases.Append(f);
                return merged;
            }



            //public IPF<TERM> RemoveCasesSubsumedBy(IPF<TERM> pf)
            //{
            //    var diff = new IPF<TERM>(pf.pfb);
            //    foreach (var f in this.cases)
            //        if (f.IsSubsumedBy(pf.cases) != Bool3.Yes)
            //            diff.cases = diff.cases.Append(f);
            //    return diff;
            //}

            public IPF<TERM> Extend(TERM guard, TERM update)
            {
                PF1 comp = null;
                foreach (var f in this.cases)
                {
                    var fg = f.Extend(guard, update);
                    if (fg != null)
                    {
                        if (comp == null)
                            comp = new PF1(fg);
                        else
                            if (fg.IsSubsumedBy(comp.cases) != Bool3.Yes)
                                comp.cases = comp.cases.Append(fg);
                    }
                }
                if (comp == null)
                    return pfb.False;
                else
                    return comp;
            }

            private PFcase MkPFcaseDirectly(TERM guard, TERM update, int k)
            {
                return new PFcase(this.pfb, guard, update, k);
            }


            private class PFcase
            {
                internal PFbuilder1<FUNC, TERM, SORT> pfb;
                IContext<FUNC, TERM, SORT> context { get { return pfb.context; } }
                TERM x { get { return pfb._x; } }
                TERM y { get { return pfb._y; } }
                TERM x_list { get { return pfb.x_list; } }
                TERM first_of_x_list { get { return pfb.first_of_x_list; } }

                internal TERM guard;
                internal TERM update;
                internal int k;

                internal PFcase(PFbuilder1<FUNC, TERM, SORT> pfb,
                    TERM guard,
                    TERM update,
                    int k)
                {
                    this.pfb = pfb;
                    this.guard = guard;
                    this.update = update;
                    this.k = k;
                }

                public PFcase(PFbuilder1<FUNC, TERM, SORT> pfb, TERM grd, TERM upd)
                {
                    this.pfb = pfb;
                    var update_depends_on_input = ContainsVar(upd, x);
                    var guard_depends_on_register = ContainsVar(grd, y);
                    if (!update_depends_on_input && !guard_depends_on_register)
                    {
                        this.guard = context.True;
                        this.update = upd;
                        this.k = 0;
                    }
                    else
                    {
                        this.guard = context.MkAnd(context.MkIsCons(x_list), context.ApplySubstitution(grd, x, first_of_x_list));
                        this.update = context.ApplySubstitution(upd, x, first_of_x_list);
                        this.k = 1;
                    }
                }

                public override bool Equals(object obj)
                {
                    PFcase pfc = obj as PFcase;
                    if (pfc == null)
                        return false;
                    return (pfc.guard.Equals(guard) && pfc.update.Equals(update));
                }

                public override int GetHashCode()
                {
                    return guard.GetHashCode() + update.GetHashCode();
                }

                public override string ToString()
                {
                    return "(" + guard.ToString() + "):(" + update.ToString() + ")";
                }

                //TERM Mk_rest_of_x_list(int n)
                //{
                //    TERM nth_rest = x_list;
                //    for (int i = 0; i < n; i++)
                //        nth_rest = context.MkRestOfList(nth_rest);
                //    return nth_rest;
                //}

                bool ContainsVar(TERM t, TERM v)
                {
                    foreach (var v1 in context.GetVars(t))
                        if (v1.Equals(v))
                            return true;
                    return false;
                }

                internal PFcase Extend(TERM guard_next, TERM update_next)
                {
                    //var kth_rest_of_x_list = Mk_rest_of_x_list(pfc.k); 
                    var upd_in = context.ApplySubstitution(this.update, x_list, pfb.rest_of_x_list);
                    var grd_in = context.ApplySubstitution(this.guard, x_list, pfb.rest_of_x_list);

                    var upd = context.Simplify(context.ApplySubstitution(update_next, y, upd_in, x, first_of_x_list));
                    var grd = context.ApplySubstitution(guard_next, y, upd_in, x, first_of_x_list);

                    var grd_comp = context.Simplify(MKAnd(grd_in, context.MkAnd(grd, pfb.is_cons_x_list)));
                    if (grd_comp.Equals(context.False))
                        return null;

                    if (!ContainsVar(upd, y) && !ContainsVar(upd, x_list))
                        return new PFcase(pfb, context.True, upd, 0);
                    else
                        return new PFcase(pfb, grd_comp, upd, this.k + 1);
                }

                TERM MKAnd(TERM pred1, TERM pred2)
                {
                    if (pred1.Equals(context.True))
                        return pred2;
                    else if (pred2.Equals(context.True))
                        return pred1;
                    else
                    {
                        var res = context.MkAnd(pred1, pred2);
                        var sat = context.IsSatisfiable(res);
                        if (sat)
                            return res;
                        else
                            return context.False;
                    }
                }

                public Bool3 IsSubsumedBy(IEnumerable<PFcase> cases)
                {
                    var negated_cases = new SimpleStack<TERM>();
                    foreach (var pfc in cases)
                        negated_cases.Push(context.MkOr(context.MkNot(pfc.guard), context.MkNeq(this.update, pfc.update)));
                    if (negated_cases.IsEmpty)
                        return Bool3.No;
                    var cond = context.MkAnd(this.guard, context.MkAnd(negated_cases));
                    var sat = context.IsSatisfiable(cond);
                    if (!sat)
                        return Bool3.Yes;
                    else
                        return Bool3.Unknown;
                }

                public TERM GetSummary()
                {
                    if (guard.Equals(context.True))
                    {
                        return context.MkEq(pfb.RegVar, context.ApplySubstitution(update, pfb.RegVar, pfb.y0));
                    }
                    else
                        return context.MkAnd(context.ApplySubstitution(guard, pfb.RegVar, pfb.y0),
                                             context.MkEq(pfb.RegVar, context.ApplySubstitution(update, pfb.RegVar, pfb.y0)));
                }
            }
        }
    }

    internal class PFbuilder2<FUNC, TERM, SORT> : IPFbuilder<TERM>
    {
        IContext<FUNC, TERM, SORT> context;
        TERM _y;
        TERM x;
        //int k = 0;
        FUNC X;
        TERM x_list;
        TERM rest_of_x_list;
        TERM first_of_x_list;
        TERM is_cons_x_list;
        TERM y_out;

        IPF<TERM> Empty;

        IPF<TERM> tt;
        override public IPF<TERM> True
        {
            get { return tt; }
        }

        override public IPF<TERM> False
        {
            get { return Empty; }
        }

        override public TERM RegVar
        {
            get { return _y; }
        }

        bool checkSubsumption = false;
        internal bool CheckSubumption
        {
            get { return checkSubsumption; }
            set { checkSubsumption = value; }
        }

        public PFbuilder2(IContext<FUNC, TERM, SORT> context, TERM x, TERM _y)
        {
            this.context = context;
            this.x = x;
            this._y = _y;
            this.X = context.MkFreshFuncDecl("X", new SORT[] { context.IntSort }, context.GetSort(x));
            this.Empty = new PF2(this);
            this.x_list = context.MkVar(0, context.MkListSort(context.GetSort(x)));
            this.rest_of_x_list = context.MkRestOfList(x_list);
            this.first_of_x_list = context.MkFirstOfList(x_list);
            this.is_cons_x_list = context.MkIsCons(x_list);
            this.y_out = context.MkVar(2, context.GetSort(_y));
            this.tt = new PF2(this, context.True, _y);
        }

        bool ContainsVar(TERM t, TERM v)
        {
            foreach (var w in context.GetVars(t))
                if (w.Equals(v))
                    return true;
            return false;
        }

        override public IPF<TERM> Create(TERM guard, TERM update)
        {
            return new PF2(this, guard, update);
        }

        class PF2 : IPF<TERM>
        {
            PFbuilder2<FUNC, TERM, SORT> pfb;

            TERM summary;
            int k;
            bool isEmpty;

            TERM Subst(TERM t, TERM key, TERM val)
            {
                return Z.ApplySubstitution(t, key, val);
            }

            TERM Subst(TERM t, TERM key1, TERM val1, TERM key2, TERM val2)
            {
                return Z.ApplySubstitution(t, key1, val1, key2, val2);
            }

            public bool Implies(IPF<TERM> rhs)
            {
                var pf = (PF2)rhs;
                var pred = Z.MkAnd(summary, Z.MkNot(pf.summary));
                bool sat = Z.IsSatisfiable(pred);
                if (!sat)
                    return true;
                else
                    return false;
            }

            public override string ToString()
            {
                return Z.PrettyPrint(summary);
            }

            public TERM Summary { get { return summary; } }

            internal PF2(PFbuilder2<FUNC, TERM, SORT> pfb)
            {
                this.pfb = pfb;
                this.summary = pfb.context.False;
                this.k = 0;
                isEmpty = true;
            }

            IContext<FUNC, TERM, SORT> Z { get { return pfb.context; } }

            bool IsFV(TERM t, TERM v)
            {
                return pfb.ContainsVar(t, v);
            }

            TERM x
            {
                get { return pfb.x; }
            }
            TERM y
            {
                get { return pfb._y; }
            }

            FUNC X
            {
                get { return pfb.X; }
            }

            public PF2(PFbuilder2<FUNC, TERM, SORT> pfb, TERM guard, TERM update)
            {
                this.pfb = pfb;
                var y_in = Z.MkFreshConst("y", Z.GetSort(y));
                this.isEmpty = false;

                if (!IsFV(guard, y) && !IsFV(update, x))
                {
                    this.summary = Z.MkEq(y, Z.ApplySubstitution(update, y, y_in));
                    this.k = 0;
                }
                else if (!IsFV(guard, x) && !IsFV(update, x))
                {
                    this.summary = Z.MkAnd(Z.ApplySubstitution(guard,y, y_in), Z.MkEq(y, Z.ApplySubstitution(update, y, y_in)));
                    this.k = 0;
                }
                else
                {
                    var guard1 = Subst(guard, x, pfb.first_of_x_list, y, y_in);
                    var update1 = Subst(update, x, pfb.first_of_x_list, y, y_in);
                    this.summary = Z.MkAnd(pfb.is_cons_x_list, guard1, Z.MkEq(y, update1));
                    this.k = 1;
                }
            }

            public IPF<TERM> Extend(TERM guard, TERM update)
            {
                if (!IsFV(guard, y) && !IsFV(update, y))
                    return new PF2(pfb, guard, update);

                var y1 = Z.MkFreshConst("y", Z.GetSort(y));

                var guard1 = Z.ApplySubstitution(guard, y, y1);
                var update1 = Z.ApplySubstitution(update, y, y1);
                var summary_prev = Z.ApplySubstitution(summary, y, y1);

                if (!IsFV(guard1, x) && !IsFV(update1, x))
                {
                    var summary1 = Z.MkAnd(summary_prev, guard1, Z.MkEq(y, update1));
                    if (!Z.IsSatisfiable(summary1))
                        return pfb.Empty;

                    var pf1 = new PF2(pfb);
                    pf1.k = k;
                    pf1.summary = summary1;
                    pf1.isEmpty = false;
                    return pf1;
                }

                var summary_prev2 = Subst(summary_prev, pfb.x_list, pfb.rest_of_x_list);
                var guard2 = Z.ApplySubstitution(guard1, x, pfb.first_of_x_list);
                var update2 = Z.ApplySubstitution(update1, x, pfb.first_of_x_list);
                var summary2 = Z.MkAnd(summary_prev2, pfb.is_cons_x_list, guard2, Z.MkEq(y, update2));
                if (!Z.IsSatisfiable(summary2))
                    return pfb.Empty;
                var pf2 = new PF2(pfb);
                pf2.k = k + 1;
                pf2.summary = summary2;
                pf2.isEmpty = false;
                return pf2;
            }

            public IPF<TERM> Merge(IPF<TERM> ipf)
            {
                var pf = (PF2)ipf;
                if (this.isEmpty)
                    return pf;
                else if (pf.isEmpty)
                    return this;
                else if (pf.Equals(pf.pfb.True) || this.Equals(pf.pfb.True))
                    return pf.pfb.True;
                else if (pfb.checkSubsumption && this.Implies(pf))
                    return pf;
                else if (pfb.checkSubsumption && pf.Implies(this))
                    return this;
                else
                {
                    var pf1 = new PF2(pfb);
                    pf1.k = Math.Max(k, pf.k);
                    pf1.summary = Z.MkOr(this.summary, pf.summary);
                    pf1.isEmpty = false;
                    return pf1;
                }
            }

            public bool IsSatisfiable
            {
               get { return !isEmpty; } 
            }
        }
    }
}
