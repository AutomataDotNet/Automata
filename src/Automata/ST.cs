using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Microsoft.Automata;

namespace Microsoft.Automata
{
    /// <summary>
    /// Symbolic Transducer (ST).
    /// Contains methods for constructing and core algorithms for manipulating STs.
    /// </summary>
    public class ST<FUNC, TERM, SORT>  : AutomatonSerializer<Rule<TERM>>, IRegisterInfo<TERM>, INameProvider,
        ITransducer<Rule<TERM>>, IAcceptor<FUNC, TERM, SORT>
    {
        internal STb<FUNC, TERM, SORT> _STb;

        /// <summary>
        /// Gets or sets the underlying STb, or null if it does not exist.
        /// </summary>
        public STb<FUNC, TERM, SORT> STb
        {
            get { return _STb; }
            set { _STb = value; }
        }

        /// <summary>
        /// Convert the ST into an STb. If the ST is nondeterministic then the transformation is incorrect.
        /// </summary>
        public STb<FUNC, TERM, SORT> ToSTb()
        {
            if (_STb != null)
                return _STb;

            var stb = new STb<FUNC, TERM, SORT>(solver, name, inputSort, outputSort, registerSort, initReg, automaton.InitialState);
            foreach (int state in automaton.States)
            {
                BranchingRule<TERM> rule = new UndefRule<TERM>();
                foreach (var r in this.GetNonFinalMovesFrom(state))
                {
                    var brule = new BaseRule<TERM>(new Sequence<TERM>(r.Label.Yields), r.Label.Update, r.TargetState);
                    if (solver.True.Equals(r.Label.Guard))
                    {
                        rule = brule;
                    }
                    else
                    {
                        rule = new IteRule<TERM>(r.Label.Guard, brule, rule);
                    }
                }
                stb.AssignRule(state, rule);
                BranchingRule<TERM> frule = UndefRule<TERM>.Default;
                foreach (var r in this.GetFinalRules(state))
                {
                    var brule = new BaseRule<TERM>(new Sequence<TERM>(r.Yields), initReg, state);
                    if (solver.True.Equals(r.Guard))
                    {
                        frule = brule;
                    }
                    else
                    {
                        frule = new IteRule<TERM>(r.Guard, brule, new UndefRule<TERM>());
                    }
                }
                stb.AssignFinalRule(state, frule);
            }
            this._STb = stb;
            stb.ST = this;
            return stb;
        }

        public override void ShowGraph(int k)
        {
            if (k <= 0)
                base.ShowGraph();
            else
            {
                var st = Explore(k);
                var aut = st.MkInstance();
                Microsoft.Automata.DirectedGraphs.DgmlWriter.ShowGraph<string>(-1, aut, this.name); 
            }
        }

        //#region C# conversion
        ///// <summary>
        ///// Converts the ST to a textual representation as a C# program.
        ///// Writes the text to the given string builder sb.
        ///// </summary>
        //public void ToCS(StringBuilder sb, string namespacename, string classname)
        //{
        //    var stb = ToSTb();
        //    stb.ToCS(sb, namespacename, classname);
        //}

        //#endregion

        string name;
        IContext<FUNC,TERM,SORT> solver;
        SORT inputSort;
        SORT outputSort;
        SORT registerSort;
        internal Automaton<Rule<TERM>> automaton;
        TERM x; //input variable
        TERM y; //register
        TERM initReg; //initial register value
        SORT inListSort;
        SORT outListSort;
        int maxLookahead;

        const string __input_variable = "x";
        const string __register_variable = "y";

        string LookupVarName(TERM term)
        {
            if (solver.GetVarIndex(term) == 0)
                return __input_variable; 
            else if (solver.GetVarIndex(term) == 1)
                return __register_variable; 
            else
                return term.ToString();
        }

        /// <summary>
        /// The associated SMT solver.
        /// </summary>
        public IContext<FUNC, TERM, SORT> Solver
        {
            get { return solver; }
        }

        public SORT InputSort { get { return inputSort; } }
        public SORT InputListSort { get { return inListSort; } }
        public SORT OutputSort { get { return outputSort; } }
        public SORT OutputListSort { get { return outListSort; } }
        public SORT RegisterSort { get { return registerSort; } }

        public TERM InitialRegister { get { return initReg; } }
        public int StateCount { get { return automaton.StateCount; } }
        public int MoveCount { get { return automaton.MoveCount; } }

        /// <summary>
        /// Enumerate all moves of nonfinal rules from the given state.
        /// </summary>
        public IEnumerable<Move<Rule<TERM>>> GetNonFinalMovesFrom(int state)
        {
            foreach (var move in automaton.GetMovesFrom(state))
                if (!move.Label.IsFinal)
                    yield return move;
        }

        /// <summary>
        /// Updates the size of the max lookahead
        /// </summary>
        public void UpdateMaxLookahead()
        {
            int max = 1;
            foreach(var state in automaton.GetStates())
                foreach (var move in automaton.GetMovesFrom(state))
                    if(move.Label.k >max)                        
                        max = move.Label.k;
            maxLookahead = max;
        }

        /// <summary>
        /// Enumerate all rules that contain the final yields from the given state.
        /// </summary>
        public IEnumerable<Rule<TERM>> GetFinalRules(int state)
        {
            if (automaton.IsFinalState(state))
                foreach (var move in automaton.GetMovesFrom(state))
                    if (move.Label.IsFinal)
                        yield return move.Label;
        }

        bool noYields;        


        /// <summary>
        /// Returns true iff the yields of all rules are empty. 
        /// In other words, the ST corresponds to an automaton with registers that does not yield any outputs.
        /// </summary>
        public bool YieldsAreEmpty
        {
            get { return noYields; }
        }

        STBuilder<FUNC, TERM, SORT> STbuilder;

        protected ST(IContext<FUNC, TERM, SORT> z3p, string name, TERM initReg, 
            SORT inputSort, SORT outputSort, SORT registerSort, Automaton<Rule<TERM>> automaton, bool noYields)
        {
            this.Name = name;
            this.initReg = initReg;
            this.solver = z3p;
            this.inputSort = inputSort;
            this.outputSort = outputSort;
            this.registerSort = registerSort;
            this.automaton = automaton;
            this.x = z3p.MkVar(0, inputSort); 
            this.y = z3p.MkVar(1, registerSort);
            this.inListSort = z3p.MkListSort(inputSort);
            this.outListSort = z3p.MkListSort(outputSort);
            this.noYields = noYields;
            this.STbuilder = new STBuilder<FUNC, TERM, SORT>(z3p);
        }

        public static ST<FUNC, TERM, SORT> Create(IContext<FUNC, TERM, SORT> solver, string name, TERM initialRegister, 
            SORT inSort, SORT outSort, SORT regSort, int initialState,
          IEnumerable<Move<Rule<TERM>>> movesAndFinalOutputs)
        {
            var aut = Automaton<Rule<TERM>>.Create(null, initialState, ExtractFinalStates(movesAndFinalOutputs),
              movesAndFinalOutputs);
            aut.EliminateUnrreachableStates();
            aut.EliminateDeadStates();
            return new ST<FUNC, TERM, SORT>(solver, name, initialRegister, inSort, outSort, regSort,
              aut, AllYieldsAreEmpty(aut.GetMoves()));
        }

        /// <summary>
        /// Cteate the identity SFT I for the sort such that I(x) = x for all sequences x of elements of the given sort.
        /// </summary>
        /// <param name="solver">underlying label theory</param>
        /// <param name="sort">input and output sort</param>
        public static ST<FUNC, TERM, SORT> MkId(IContext<FUNC, TERM, SORT> solver, SORT sort)
        {
            var moves = new Move<Rule<TERM>>[]{
                new Move<Rule<TERM>>(0,0,Rule<TERM>.Mk(solver.True, solver.UnitConst, solver.MkVar(0, sort))),
                new Move<Rule<TERM>>(0,0,Rule<TERM>.MkFinal(solver.True))};
            return new ST<FUNC, TERM, SORT>(solver, "Id", solver.UnitConst, sort, sort, solver.UnitSort,
              Automaton<Rule<TERM>>.Create(null, 0, new int[] { 0 }, moves), false);
        }


        //internal ST<FUNC, TERM, SORT> ShiftYields()
        //{
        //    foreach (int s in automaton.States)
        //    {
        //        if ((s != automaton.InitialState) && (!automaton.IsFinalState(s)) && (!automaton.IsLoopState(s)))
        //        {
        //            automaton.Re
        //        }
        //    }
        //}

        //int ChooseRemovable



        #region Axioms

        bool theoryIsAsserted = false;


        /// <summary>
        /// Defines axioms for nil and cons separately.
        /// </summary>
        public void AssertTheory()
        {
            if (!theoryIsAsserted)
            {
                acceptor = DefineSTAcceptor();
                theoryIsAsserted = true;
            }
        }

        bool prodTheoryAsserted = false;
        FUNC prodAcceptor = default(FUNC);
        public void AssertProdTheory()
        {
            if (!prodTheoryAsserted)
            {
                prodAcceptor = DefineProdSTAcceptor();
                prodTheoryAsserted = true;
            }
        }

        /// <summary>
        /// Uses most general patterns for axioms.
        /// </summary>
        public void AssertTheory2()
        {
            if (!theoryIsAsserted)
            {
                acceptor = DefineSTAcceptor2();
                theoryIsAsserted = true;
            }
        }

        /// <summary>
        /// Uses fixed patterns nil and cons(x,w) for the input list. Guarantees termination.
        /// </summary>
        /// <returns></returns>
        FUNC DefineSTAcceptor()
        {
            TERM nil = solver.GetNil(inListSort);
            TERM nilOut = solver.GetNil(outListSort);
            // input [x|w], output z, register y
            TERM w = solver.MkVar(2, inListSort);
            TERM z = solver.MkVar(3, outListSort);
            TERM xw = solver.MkListCons(x, w);
            // in base case, output z0
            TERM z0 = solver.MkVar(0, outListSort);

            FUNC mainAcc = solver.MkFreshFuncDecl(name,
                    new SORT[] { inListSort, outListSort }, solver.BoolSort);

            Dictionary<int, FUNC> acceptDecls = new Dictionary<int, FUNC>();
            foreach (int state in automaton.States)
                acceptDecls[state] = solver.MkFreshFuncDecl(name,
                      new SORT[] { inListSort, outListSort, registerSort }, solver.BoolSort);

            #region create axioms
            foreach (int state in automaton.States)
            {
                #region base case
                var finals = new List<Rule<TERM>>(GetFinalRules(state));
                TERM lhs0 = solver.MkApp(acceptDecls[state], nil, z0, y);
                List<TERM> finalConds = new List<TERM>();
                foreach (var final in finals)
                    finalConds.Add(solver.MkAnd(final.Guard, solver.MkEq(z0, solver.MkListWithRest(final.Yields, nilOut))));
                TERM rhs0 = (finalConds.Count == 0 ? solver.False : (finalConds.Count == 1 ? finalConds[0] : solver.MkOr(finalConds.ToArray())));

                solver.MainSolver.Assert(solver.MkAxiom(lhs0, rhs0, z0, y));
                #endregion

                #region recusion case
                var rules = new List<Move<Rule<TERM>>>(GetNonFinalMovesFrom(state));
                TERM lhs = solver.MkApp(acceptDecls[state], xw, z, y);
                List<TERM> rhs_cases = new List<TERM>();

                #region collect rhs cases
                foreach (Move<Rule<TERM>> t in rules)
                {
                    if (t.IsEpsilon)
                        throw new AutomataException(AutomataExceptionKind.EpsilonMovesAreNotSupportedInSTs);

                    else
                    {
                        var yields = t.Label.Yields;
                        int n = yields.Length;
                        //take the i'th tail of v
                        //take the i'th elem of v
                        TERM[] hd_v = new TERM[n];
                        TERM[] tl_v = new TERM[n];
                        for (int i = 0; i < n; i++)
                        {
                            tl_v[i] = (i == 0 ? z : solver.MkRestOfList(tl_v[i - 1]));
                            hd_v[i] = solver.MkFirstOfList(tl_v[i]);
                        }
                        List<TERM> conjuncts = new List<TERM>();
                        conjuncts.Add(t.Label.Guard);
                        for (int i = 0; i < n; i++)
                        {
                            conjuncts.Add(solver.MkNeq(tl_v[i], nil));
                            conjuncts.Add(solver.MkEq(hd_v[i], yields[i]));
                        }
                        conjuncts.Add(solver.MkApp(acceptDecls[t.TargetState], w, (n == 0 ? z : solver.MkRestOfList(tl_v[n - 1])), t.Label.Update));
                        rhs_cases.Add(solver.MkAnd(conjuncts.ToArray()));
                    }
                }
                #endregion

                TERM rhs = solver.MkOr(rhs_cases.ToArray());

                solver.MainSolver.Assert(solver.MkAxiom(lhs, rhs, x, y, w, z));
                #endregion recursion case
            }

            #region main axiom ax
            TERM inList = solver.MkVar(0, inListSort);
            TERM outList = solver.MkVar(1, outListSort);
            TERM lhsMain = solver.MkApp(mainAcc, inList, outList);
            TERM rhsMain = solver.MkApp(acceptDecls[automaton.InitialState], inList, outList, initReg);
            solver.MainSolver.Assert(solver.MkAxiom(lhsMain, rhsMain, inList, outList));
            #endregion

            #endregion

            return mainAcc;
        }

        /// <summary>
        /// Uses the most general pattern for axioms without constraints on argument lists.
        /// Relies on the solvers internal proof search engine to guarantee termination.
        /// </summary>
        FUNC DefineSTAcceptor2()
        {
            TERM nil = solver.GetNil(inListSort);
            TERM nilOut = solver.GetNil(outListSort);
            // input w, output z, register y
            TERM xw = solver.MkVar(0, inListSort);
            TERM z = solver.MkVar(2, outListSort);
            TERM x = solver.MkFirstOfList(xw);
            TERM w = solver.MkRestOfList(xw);

            FUNC mainAcc = solver.MkFreshFuncDecl(name,
                    new SORT[] { inListSort, outListSort }, solver.BoolSort);

            Dictionary<int, FUNC> acceptDecls = new Dictionary<int, FUNC>();
            foreach (int state in automaton.States)
                acceptDecls[state] = solver.MkFreshFuncDecl(name,
                      new SORT[] { inListSort, outListSort, registerSort }, solver.BoolSort);

            foreach (int state in automaton.States)
            {
                #region base case xw = nil
                var finals = new List<Rule<TERM>>(GetFinalRules(state));
                List<TERM> finalConds = new List<TERM>();
                foreach (var final in finals)
                    finalConds.Add(solver.MkAnd(final.Guard, solver.MkEq(z, solver.MkListWithRest(final.Yields, nilOut))));
                TERM rhs0 = (finalConds.Count == 0 ? solver.False : (finalConds.Count == 1 ? finalConds[0] : solver.MkOr(finalConds.ToArray())));
                rhs0 = solver.MkAnd(solver.MkEq(xw, nil), rhs0);
                #endregion

                #region recusion case xw != nil
                var rules = new List<Move<Rule<TERM>>>(GetNonFinalMovesFrom(state));
                TERM lhs = solver.MkApp(acceptDecls[state], xw, z, y);
                List<TERM> rhs_cases = new List<TERM>();

                TERM x0 = solver.MkVar(0, inputSort);

                #region collect rhs cases
                foreach (Move<Rule<TERM>> t in rules)
                {
                    if (t.IsEpsilon)
                        throw new AutomataException(AutomataExceptionKind.EpsilonMovesAreNotSupportedInSTs);

                    else
                    {
                        var yields1 = t.Label.Yields;
                        var yields = Array.ConvertAll<TERM, TERM>(yields1, y0 => solver.ApplySubstitution(y0, x0, x));
                        int n = yields.Length;
                        //take the i'th tail of v
                        //take the i'th elem of v
                        TERM[] hd_v = new TERM[n];
                        TERM[] tl_v = new TERM[n];
                        for (int i = 0; i < n; i++)
                        {
                            tl_v[i] = (i == 0 ? z : solver.MkRestOfList(tl_v[i - 1]));
                            hd_v[i] = solver.MkFirstOfList(tl_v[i]);
                        }
                        List<TERM> conjuncts = new List<TERM>();
                        TERM guard = solver.ApplySubstitution(t.Label.Guard, x0, x);
                        TERM update = solver.ApplySubstitution(t.Label.Update, x0, x);
                        conjuncts.Add(guard);
                        for (int i = 0; i < n; i++)
                        {
                            conjuncts.Add(solver.MkNeq(tl_v[i], nil));
                            conjuncts.Add(solver.MkEq(hd_v[i], yields[i]));
                        }
                        conjuncts.Add(solver.MkApp(acceptDecls[t.TargetState], w, (n == 0 ? z : solver.MkRestOfList(tl_v[n - 1])), update));
                        rhs_cases.Add(solver.MkAnd(conjuncts.ToArray()));
                    }
                }
                #endregion

                TERM rhs1 = solver.MkOr(rhs_cases.ToArray());
                rhs1 = solver.MkAnd(solver.MkNeq(xw, nil), rhs1);

                TERM rhs = solver.MkOr(rhs0, rhs1);

                solver.MainSolver.Assert(solver.MkAxiom(lhs, rhs, xw, y, z));
                #endregion recursion case
            }

            #region main axiom
            TERM inList = solver.MkVar(0, inListSort);
            TERM outList = solver.MkVar(1, outListSort);
            TERM lhsMain = solver.MkApp(mainAcc, inList, outList);
            TERM rhsMain = solver.MkApp(acceptDecls[automaton.InitialState], inList, outList, initReg);
            solver.MainSolver.Assert(solver.MkAxiom(lhsMain, rhsMain, inList, outList));
            #endregion

            return mainAcc;
        }


        /// <summary>
        /// Uses the most general pattern for axioms without constraints on argument lists.
        /// Defines an acceptor with two outputs for the product case.
        /// </summary>
        FUNC DefineProdSTAcceptor()
        {
            TERM nil = solver.GetNil(inListSort);
            TERM nilOut = solver.GetNil(outListSort);
            // input w, output z, register y
            TERM xw = solver.MkVar(0, inListSort);
            TERM z1 = solver.MkVar(2, outListSort);
            TERM z2 = solver.MkVar(3, outListSort);
            TERM x = solver.MkFirstOfList(xw);
            TERM w = solver.MkRestOfList(xw);

            FUNC mainAcc = solver.MkFreshFuncDecl(name,
                    new SORT[] { inListSort, outListSort, outListSort }, solver.BoolSort);

            Dictionary<int, FUNC> acceptDecls = new Dictionary<int, FUNC>();
            foreach (int state in automaton.States)
                acceptDecls[state] = solver.MkFreshFuncDecl(name,
                      new SORT[] { inListSort, outListSort, outListSort, registerSort }, solver.BoolSort);

            foreach (int state in automaton.States)
            {
                #region base case xw = nil
                var finals = new List<Rule<TERM>>(GetFinalRules(state));
                List<TERM> finalConds = new List<TERM>();
                foreach (var final in finals)
                    finalConds.Add(solver.MkAnd(solver.MkAnd(final.Guard, solver.MkEq(z1, solver.MkListWithRest(final.Yields, nilOut))), solver.MkEq(z2, solver.MkListWithRest(final.Yields2, nilOut))));
                TERM rhs0 = (finalConds.Count == 0 ? solver.False : (finalConds.Count == 1 ? finalConds[0] : solver.MkOr(finalConds.ToArray())));
                rhs0 = solver.MkAnd(solver.MkEq(xw, nil), rhs0);
                #endregion

                #region recusion case xw != nil
                var rules = new List<Move<Rule<TERM>>>(GetNonFinalMovesFrom(state));
                TERM lhs = solver.MkApp(acceptDecls[state], xw, z1, z2, y);
                List<TERM> rhs_cases = new List<TERM>();

                TERM x0 = solver.MkVar(0, inputSort);

                #region collect rhs cases
                foreach (Move<Rule<TERM>> t in rules)
                {
                    if (t.IsEpsilon)
                        throw new AutomataException(AutomataExceptionKind.EpsilonMovesAreNotSupportedInSTs);

                    else
                    {
                        var yields = Array.ConvertAll<TERM, TERM>(t.Label.Yields, y0 => solver.ApplySubstitution(y0, x0, x));
                        var yields2 = Array.ConvertAll<TERM, TERM>(t.Label.Yields2, y0 => solver.ApplySubstitution(y0, x0, x));
                        int n = yields.Length;
                        int n2 = yields2.Length;
                        //take the i'th tail of v
                        //take the i'th elem of v
                        TERM[] hd_v = new TERM[n];
                        TERM[] tl_v = new TERM[n];
                        for (int i = 0; i < n; i++)
                        {
                            tl_v[i] = (i == 0 ? z1 : solver.MkRestOfList(tl_v[i - 1]));
                            hd_v[i] = solver.MkFirstOfList(tl_v[i]);
                        }
                        TERM[] hd_v2 = new TERM[n2];
                        TERM[] tl_v2 = new TERM[n2];
                        for (int i = 0; i < n2; i++)
                        {
                            tl_v2[i] = (i == 0 ? z2 : solver.MkRestOfList(tl_v2[i - 1]));
                            hd_v2[i] = solver.MkFirstOfList(tl_v2[i]);
                        }
                        List<TERM> conjuncts = new List<TERM>();
                        TERM guard = solver.ApplySubstitution(t.Label.Guard, x0, x);
                        TERM update = solver.ApplySubstitution(t.Label.Update, x0, x);
                        conjuncts.Add(guard);
                        for (int i = 0; i < n; i++)
                        {
                            conjuncts.Add(solver.MkNeq(tl_v[i], nil));
                            conjuncts.Add(solver.MkEq(hd_v[i], yields[i]));
                        }
                        for (int i = 0; i < n2; i++)
                        {
                            conjuncts.Add(solver.MkNeq(tl_v2[i], nil));
                            conjuncts.Add(solver.MkEq(hd_v2[i], yields2[i]));
                        }
                        conjuncts.Add(solver.MkApp(acceptDecls[t.TargetState], w, (n == 0 ? z1 : solver.MkRestOfList(tl_v[n - 1])), (n2 == 0 ? z2 : solver.MkRestOfList(tl_v2[n2 - 1])), update));
                        rhs_cases.Add(solver.MkAnd(conjuncts.ToArray()));
                    }
                }
                #endregion

                TERM rhs1 = solver.MkOr(rhs_cases.ToArray());
                rhs1 = solver.MkAnd(solver.MkNeq(xw, nil), rhs1);

                TERM rhs = solver.MkOr(rhs0, rhs1);

                solver.MainSolver.Assert(solver.MkAxiom(lhs, rhs, xw, y, z1, z2));
                #endregion recursion case
            }

            #region main axiom
            TERM inList = solver.MkVar(0, inListSort);
            TERM outList = solver.MkVar(1, outListSort);
            TERM outList2 = solver.MkVar(2, outListSort);
            TERM lhsMain = solver.MkApp(mainAcc, inList, outList, outList2);
            TERM rhsMain = solver.MkApp(acceptDecls[automaton.InitialState], inList, outList, outList2, initReg);
            solver.MainSolver.Assert(solver.MkAxiom(lhsMain, rhsMain, inList, outList, outList2));
            #endregion

            return mainAcc;
        }

        FUNC acceptor;

        public FUNC Acceptor
        {
            get
            {
                if (!theoryIsAsserted)
                    throw new AutomataException(AutomataExceptionKind.TheoryIsNotAsserted);

                return acceptor;
            }
        }

        /// <summary>
        /// Constructs the transducer acceptor atom 'Acceptor(inList, outList)' for the ST.
        /// Assumes that the theory has been asserted and is in scope.
        /// </summary>
        /// <param name="inList">any term of sort InputListSort</param>
        /// <param name="outList">any term of sort OutputListSort</param>
        public TERM MkAccept(TERM inList, TERM outList)
        {
            if (!solver.GetSort(inList).Equals(inListSort) || !solver.GetSort(outList).Equals(outListSort))
                throw new AutomataException(AutomataExceptionKind.SortMismatch);

            if (!theoryIsAsserted)
                throw new AutomataException(AutomataExceptionKind.TheoryIsNotAsserted);

            return solver.MkApp(acceptor, inList, outList);
        }

        public TERM MkProdAccept(TERM inList, TERM outList1, TERM outList2)
        {
            if (!solver.GetSort(inList).Equals(inListSort) || !solver.GetSort(outList1).Equals(outListSort) || !solver.GetSort(outList2).Equals(outListSort))
                throw new AutomataException(AutomataExceptionKind.SortMismatch);

            if (!prodTheoryAsserted)
                throw new AutomataException(AutomataExceptionKind.TheoryIsNotAsserted);

            return solver.MkApp(prodAcceptor, inList, outList1, outList2);
        }

        #endregion

        #region IAutomaton<Rule> Members

        public int InitialState
        {
            get { return automaton.InitialState; }
        }

        public bool IsFinalState(int state)
        {
            return automaton.IsFinalState(state);
        }

        public IEnumerable<Move<Rule<TERM>>> GetMoves()
        {
            return automaton.GetMoves();
        }

        public IEnumerable<Move<Rule<TERM>>> GetMovesFrom(int state)
        {
            return automaton.GetMovesFrom(state);
        }

        public IEnumerable<int> GetStates()
        {
            return automaton.GetStates();
        }

        public string DescribeState(int state)
        {
            return automaton.DescribeState(state);
        }

        /// <summary>
        /// Describes the rule as pp(Guard)/[pp(Yields)];pp(Update)
        /// where pp is the given TERM pretty printer and where ';pp(Update)' is omitted if Update=null.
        /// If the label is final, the guard is true, and there are no yields then the output is the empty string.
        /// If the label is final the the update is omitted, because then Update=null..
        /// </summary>
        public string DescribeLabel(Rule<TERM> lab)
        {
            if (lab.IsFinal && lab.Guard.Equals(solver.True) && lab.Yields.Length == 0)
                return "";

            string s = ""; //(lab.IsFinal ? "{" : "");
            s += DescribeGuard(lab);
            s += "/";
            s += DescribeYields(lab);
            var upd = DescribeUpdate(lab);
            s += (upd == "" ? "" : ";" + upd);
            return s;
        }

        #endregion

        #region composition

        /// <summary>
        /// Creates a symbolic composition of <paramref name="A"/> with <paramref name="B"/>.
        /// Such that forall s, A+B(s) = B(A(s))
        /// </summary>
        /// <param name="A">first ST</param>
        /// <param name="B">second ST</param>
        /// <returns>A+B is such that A+B(s) = B(A(s)) for all s</returns>
        public static ST<FUNC, TERM, SORT> operator +(ST<FUNC, TERM, SORT> A, ST<FUNC, TERM, SORT> B)
        {
            return Compose(A, B);
        }

        /// <summary>
        /// Returns true iff the ST is an SFT, i.e., the register sort is unitsort (register is not used).
        /// </summary>
        public bool IsRegisterFree
        {
            get
            {
                return (object.Equals(RegisterSort, solver.UnitSort));
            }
        }

        /// <summary>
        /// Composes this=A with B, the result is A+B such that A+B(s) = B(A(s)) for all s.
        /// </summary>
        public ST<FUNC, TERM, SORT> Compose(ST<FUNC, TERM, SORT> B)
        {
            return Compose(this, B);
        }

        /// <summary>
        /// Makes a parallel product composition of this ST with A.
        /// Assumes that this and B have no outputs (all yields are empty).
        /// </summary>
        /// <param name="B"></param>
        /// <returns></returns>
        public ST<FUNC, TERM, SORT> Product(ST<FUNC, TERM, SORT> B)
        {
            var A = ToIdOutput();
            var AxB = Compose(A, B);
            AxB.name = string.Format("{0}_x_{1}", A.Name, B.Name);
            return AxB;
        }

        /// <summary>
        /// Composes A with B, the result is A+B such that A+B(s) = B(A(s)) for all s.
        /// </summary>
        public static ST<FUNC, TERM, SORT> Compose(ST<FUNC, TERM, SORT> A, ST<FUNC, TERM, SORT> B)
        {
            if (A == null || B == null)
                throw new ArgumentNullException();

            if (A.Solver != B.Solver)
                throw new AutomataException(AutomataExceptionKind.SolversAreNotIdentical);

            if (!A.OutputSort.Equals(B.InputSort))
                throw new AutomataException(AutomataExceptionKind.SortMismatchInComposition);

            IContext<FUNC, TERM, SORT> Z = A.Solver;

            #region initialize
            var autA = A.automaton;
            var autB = B.automaton;

            var _iA = Z.MkVar(0, A.InputSort);
            var _iB = Z.MkVar(0, B.InputSort);
            var _rA = Z.MkVar(1, A.RegisterSort);
            var _rB = Z.MkVar(1, B.RegisterSort);
            var regSort = (A.IsRegisterFree ? B.RegisterSort : (B.IsRegisterFree ? A.RegisterSort : Z.MkTupleSort(A.RegisterSort, B.RegisterSort)));
            var r = Z.MkVar(1, regSort);
            var rA = (A.IsRegisterFree ? Z.UnitConst : (B.IsRegisterFree ? r : Z.MkProj(0, r)));
            var rB = (B.IsRegisterFree ? Z.UnitConst : (A.IsRegisterFree ? r : Z.MkProj(1, r)));

            int _id = 1;
            var initState = new Tuple<int, int>(autA.InitialState, autB.InitialState);
            var stateIdMap = new Dictionary<Tuple<int, int>, int>();
            stateIdMap[initState] = 0;
            var visited = new HashSet<Tuple<int, int>>();
            visited.Add(initState);
            var moves = new List<Move<Rule<TERM>>>();
            var stack = new Stack<Tuple<int, int>>();
            stack.Push(initState);
            var finalStates = new HashSet<int>();
            if (autA.IsFinalState(autA.InitialState) && autB.IsFinalState(autB.InitialState))
                finalStates.Add(0);
            #endregion

            #region helper methods

            Func<Tuple<int, int>, int> GetStateId = p =>
            {
                int id;
                if (!stateIdMap.TryGetValue(p, out id))
                {
                    id = _id;
                    _id = _id + 1;
                    stateIdMap[p] = id;
                }
                return id;
            };

            Func<TERM, TERM, TERM> MkTuple = (a, b) =>
            {
                if (A.IsRegisterFree)
                    return b;
                else if (B.IsRegisterFree)
                    return a;
                else
                    return Z.MkTuple(a, b);
            };

            Func<TERM, TERM> ProjA = p =>
            {
                if (A.IsRegisterFree)
                    return Z.UnitConst;
                else if (B.IsRegisterFree)
                    return p;
                else
                    return Z.MkProj(0, p);
            };

            Func<TERM, TERM> ProjB = p =>
            {
                if (B.IsRegisterFree)
                    return Z.UnitConst;
                else if (A.IsRegisterFree)
                    return p;
                else
                    return Z.MkProj(1, p);
            };


            Func<Tuple<int, int>, bool> AddToSearch = p =>
            {
                if (visited.Add(p))
                {
                    stack.Push(p);
                    int pId = GetStateId(p);
                    if (autA.IsFinalState(p.Item1) && autB.IsFinalState(p.Item2))
                        finalStates.Add(pId);
                    return true;
                }
                return false;
            };

            #endregion

            var eps = new TERM[] { };

            while (stack.Count > 0)
            {
                var curr = stack.Pop();
                var source = GetStateId(curr);

                foreach (var mA in A.GetNonFinalMovesFrom(curr.Item1))
                {
                    if (mA.IsEpsilon)
                        throw new AutomataException(AutomataExceptionKind.EpsilonMovesAreNotSupportedInSTs);

                    TERM aGuard = Z.ApplySubstitution(mA.Label.Guard, _rA, rA);
                    TERM aUpdate = Z.ApplySubstitution(mA.Label.Update, _rA, rA);
                    TERM[] aYields = Array.ConvertAll<TERM, TERM>(mA.Label.Yields, y => Z.ApplySubstitution(y, _rA, rA));

                    if (mA.Label.Yields.Length == 0) //the output from A is eps
                    {
                        var sp = new Tuple<int,int>(mA.TargetState, curr.Item2);
                        AddToSearch(sp);
                        Rule<TERM> composedLabel = Rule<TERM>.Mk(aGuard, MkTuple(aUpdate, rB));
                        moves.Add(Move<Rule<TERM>>.Create(source, GetStateId(sp), composedLabel));
                        continue;
                    }

                    //int time = System.Environment.TickCount;
                    var paths = new List<Tuple<int, Rule<TERM>>>(GetForwardPaths(Z, aGuard, aYields, 0, aYields.Length, curr.Item2, rB, _rB, _iB, B));
                    //time = System.Environment.TickCount - time;

                    foreach (var path in paths)
                    {
                        var sp = new Tuple<int, int>(mA.TargetState, path.Item1);
                        AddToSearch(sp);
                        Rule<TERM> composedLabel = Rule<TERM>.Mk(path.Item2.Guard, MkTuple(aUpdate, path.Item2.Update), path.Item2.Yields);
                        moves.Add(Move<Rule<TERM>>.Create(source, GetStateId(sp), composedLabel));
                    }
                }

                foreach (var finalA in A.GetFinalRules(curr.Item1))
                {
                    TERM aGuard = Z.ApplySubstitution(finalA.Guard, _rA, rA);
                    TERM[] aYields = Array.ConvertAll<TERM, TERM>(finalA.Yields, y => Z.ApplySubstitution(y, _rA, rA));

                    if (aYields.Length == 0)
                    {
                        foreach (var finalB in B.GetFinalRules(curr.Item2))
                        {
                            TERM bGuard = Z.ApplySubstitution(finalB.Guard, _rB, rB);
                            TERM[] bYields = Array.ConvertAll<TERM, TERM>(finalB.Yields, y => Z.ApplySubstitution(y, _rB, rB));
                            moves.Add(MkFinalOutput(Z, source, Z.MkAnd(aGuard, bGuard), bYields));
                        }
                    }
                    else
                    {
                        var paths = new List<Tuple<int, Rule<TERM>>>(GetForwardPaths(Z, aGuard, aYields, 0, aYields.Length, curr.Item2, rB, _rB, _iB, B));
                        foreach (var path in paths)
                        {
                            foreach (var finalB in B.GetFinalRules(path.Item1))
                            {
                                TERM bGuard = Z.ApplySubstitution(finalB.Guard, _rB, path.Item2.Update);
                                TERM[] bYields = Array.ConvertAll<TERM, TERM>(finalB.Yields, y => Z.ApplySubstitution(y, _rB, path.Item2.Update));
                                TERM finalGuard = Z.MkAnd(path.Item2.Guard, bGuard);
                                if (Z.IsSatisfiable(finalGuard))
                                {
                                    List<TERM> finalYields = new List<TERM>(path.Item2.Yields);
                                    finalYields.AddRange(bYields);
                                    moves.Add(MkFinalOutput(Z, source, finalGuard, finalYields.ToArray()));
                                }
                            }
                        }
                    }
                }
            }
            TERM initReg = MkTuple(A.InitialRegister, B.InitialRegister);

            string name = string.Format("{0}_o_{1}", A.Name, B.Name);
            ST<FUNC, TERM, SORT> AB = new ST<FUNC, TERM, SORT>(Z, name, initReg, A.InputSort, B.OutputSort, regSort,
              Automaton<Rule<TERM>>.Create(null, 0, ExtractFinalStates(moves), moves), AllYieldsAreEmpty(moves));
            AB.automaton.EliminateDeadStates();
            return AB;
        }

        internal static IEnumerable<int> ExtractFinalStates(IEnumerable<Move<Rule<TERM>>> moves)
        {
            foreach (var move in moves)
                if (move.Label.IsFinal)
                    yield return move.TargetState;
        }

        static bool AllYieldsAreEmpty(IEnumerable<Move<Rule<TERM>>> moves)
        {
            foreach (var move in moves)
                if (move.Label.Yields.Length > 0)
                    return false;
            return true;
        }

        static Move<Rule<TERM>> MkFinalOutput(IContext<FUNC,TERM,SORT> z3p, int finalState, TERM finalCondition, params TERM[] finalYields)
        {
            return Move<Rule<TERM>>.Create(finalState, finalState, Rule<TERM>.MkFinal(finalCondition, finalYields));
        }

        static IEnumerable<Tuple<int, Rule<TERM>>> GetForwardPaths(IContext<FUNC, TERM, SORT> z3p, TERM guard, TERM[] aYields, int i, int k, int state, TERM reg, TERM _rB, TERM _iB, ST<FUNC, TERM, SORT> B)
        {
            if (i == k)
                yield return new Tuple<int, Rule<TERM>>(state, Rule<TERM>.Mk(guard, reg));
            else
                foreach (var rule in B.GetNonFinalMovesFrom(state))
                {
                    TERM g = z3p.MkAnd(guard, z3p.ApplySubstitution(rule.Label.Guard, _iB, aYields[i], _rB, reg));
                    if (z3p.IsSatisfiable(g))
                    {
                        TERM r = z3p.ApplySubstitution(rule.Label.Update, _iB, aYields[i], _rB, reg);
                        TERM[] y = Array.ConvertAll(rule.Label.Yields, e => z3p.ApplySubstitution(e, _iB, aYields[i], _rB, reg));
                        foreach (var path in GetForwardPaths(z3p, g, aYields, i + 1, k, rule.TargetState, r, _rB, _iB, B))
                        {
                            TERM g1 = path.Item2.Guard;
                            TERM r1 = path.Item2.Update;
                            TERM[] y2 = path.Item2.Yields;
                            TERM[] y1 = new TERM[y.Length + y2.Length];
                            if (y.Length > 0)
                                Array.Copy(y, 0, y1, 0, y.Length);
                            if (y2.Length > 0)
                                Array.Copy(y2, 0, y1, y.Length, y2.Length);
                            Rule<TERM> lab = Rule<TERM>.Mk(g1, r1, y1);
                            yield return new Tuple<int, Rule<TERM>>(path.Item1, lab);
                        }
                    }
                }
        }

        #endregion

        #region domain restriction
        /// <summary>
        /// Restrict the domain of the ST with respect to the given regex.
        /// </summary>
        /// <param name="regex">defines the domain restriction</param>
        /// <returns>ST that accepts input lists also accepted by the regex</returns>
        public ST<FUNC, TERM, SORT> RestrictDomain(string regex)
        {
            var fa = solver.RegexConverter.Convert(regex);
            SFA<FUNC, TERM, SORT> sfa = new SFA<FUNC, TERM, SORT>(solver, inputSort, fa);
            return RestrictDomain(sfa);
        }

        /// <summary>
        /// Restrict the domain of the ST with respect to the given SFA.
        /// </summary>
        /// <param name="sfa">defines the domain restriction</param>
        /// <returns>ST that accepts input lists also accepted by the SFA</returns>
        public ST<FUNC, TERM, SORT> RestrictDomain(SFA<FUNC, TERM, SORT> sfa)
        {
            if (!solver.Equals(sfa.Solver))
                throw new AutomataException(AutomataExceptionKind.SolversAreNotIdentical);

            if (!inputSort.Equals(sfa.InputSort))
                throw new AutomataException(AutomataExceptionKind.InputSortsMustBeIdentical);

            var D = Automaton<Rule<TERM>>.Create(null, sfa.InitialState, sfa.Automaton.GetFinalStates(),
                LiftMoves(sfa.Solver, sfa.Automaton));

            var restr = Automaton<Rule<TERM>>.MkProduct_(automaton, D, new RestrictionSolver(solver));

            return new ST<FUNC, TERM, SORT>(solver, string.Format("Restr_{0}_{1}", name, sfa.Name), initReg, inputSort, outputSort, registerSort, restr, AllYieldsAreEmpty(restr.GetMoves()));
        }

        static IEnumerable<Move<Rule<TERM>>> LiftMoves(IContext<FUNC, TERM, SORT> solver, Automaton<TERM> automaton)
        {
            var eps = new TERM[] { };
            var aut = automaton.RemoveEpsilons(); //epsilon moves are not allowed in STs
            foreach (var move in aut.GetMoves())
            {
                yield return Move<Rule<TERM>>.Create(move.SourceState, move.TargetState,
                    Rule<TERM>.Mk(move.Label, solver.UnitConst));
            }
            foreach (int state in automaton.GetFinalStates())
            {
                yield return Move<Rule<TERM>>.Create(state, state, Rule<TERM>.MkFinal(solver.True));
            }
        }

        private class RestrictionSolver : IBooleanAlgebraPositive<Rule<TERM>>
        {
            IBooleanAlgebra<TERM> solver;
            public RestrictionSolver(IBooleanAlgebra<TERM> solver)
            {
                this.solver = solver;
            }

            #region disjunction is not used because epsilon transitions are not allowed
            public Rule<TERM> MkOr(Rule<TERM> r1, Rule<TERM> r2)
            {
                //cannot occur, unless there is a bug
                throw new AutomataException(AutomataExceptionKind.InternalError);
            }
            #endregion

            //non-symmetrical, assumes the second label corresponds to an SFA label
            public Rule<TERM> MkAnd(Rule<TERM> r1, Rule<TERM> r2)
            {
                if (!r1.IsFinal && !r2.IsFinal)
                    return Rule<TERM>.Mk(solver.MkAnd(r1.Guard, r2.Guard), r1.Update, r1.Yields);
                else if (r1.IsFinal && r2.IsFinal)
                    return r1;
                else
                    return Rule<TERM>.Mk(solver.False, r1.Update);
            }

            public bool IsSatisfiable(Rule<TERM> rule)
            {
                return solver.IsSatisfiable(rule.Guard);
            }


            public Rule<TERM> True
            {
                get { throw new NotImplementedException(); }
            }

            public Rule<TERM> False
            {
                get { throw new NotImplementedException(); }
            }
        }
        #endregion

        #region yield elimination

        /// <summary>
        /// Replaces all outputs by empty outputs.
        /// </summary>
        public ST<FUNC, TERM, SORT> RemoveYields()
        {
            if (noYields)
                return this;
            else
            {
                var st = new ST<FUNC, TERM, SORT>(solver, string.Format("RY[{0}]", name), initReg, inputSort, outputSort, registerSort,
                    Automaton<Rule<TERM>>.Create(null, automaton.InitialState, automaton.GetFinalStates(), EliminateYields(automaton.GetMoves())), true);
                st.isMultiInput = isMultiInput;
                return st;
            }
        }

        static IEnumerable<Move<Rule<TERM>>> EliminateYields(IEnumerable<Move<Rule<TERM>>> moves)
        {
            foreach (var move in moves)
                if (move.IsEpsilon)
                    throw new AutomataException(AutomataExceptionKind.EpsilonMovesAreNotSupportedInSTs);
                else if (move.Label.IsFinal)
                    yield return Move<Rule<TERM>>.Create(move.SourceState, move.TargetState, Rule<TERM>.MkFinal(move.Label.Guard));//???
                else
                    yield return Move<Rule<TERM>>.Create(move.SourceState, move.TargetState, Rule<TERM>.Mk(move.Label.Guard, move.Label.Update));
        }
        #endregion

        #region pre-image computation

        public ST<FUNC, TERM, SORT> ComputePreImage(ST<FUNC, TERM, SORT> rangeRestriction)
        {
            var R = rangeRestriction.RemoveYields();
            var A = Compose(this, R);
            return A;
        }

        public ST<FUNC, TERM, SORT> ComputePreImage(string regex)
        {
            var autom = solver.RegexConverter.Convert(regex);
            var sfa = new SFA<FUNC, TERM, SORT>(solver, outputSort, autom);
            return ComputePreImage(sfa);
        }

        public ST<FUNC, TERM, SORT> ComputePreImage(SFA<FUNC, TERM, SORT> rangeRestriction)
        {
            var R = SFAtoST(rangeRestriction);
            var A = Compose(this, R);
            return A;
        }

        /// <summary>
        /// Lift the sfa into an ST with initial register sfa.Solver.UnitConst, and register and output sort sfa.Solver.UnitSort.
        /// </summary>
        public static ST<FUNC, TERM, SORT> SFAtoST(SFA<FUNC, TERM, SORT> sfa)
        {
            var A = Automaton<Rule<TERM>>.Create(null, sfa.InitialState, sfa.Automaton.GetFinalStates(), LiftMoves(sfa.Solver, sfa.Automaton));

            return new ST<FUNC, TERM, SORT>(sfa.Solver, string.Format("ST[{0}]", sfa.Name), 
                sfa.Solver.UnitConst, sfa.InputSort, sfa.Solver.UnitSort, sfa.Solver.UnitSort, A, true);
        }

        /// <summary>
        /// Register variable is Solver.MkVar(1, RegisterSort).
        /// </summary>
        public TERM RegisterVar { get { return solver.MkVar(1, registerSort); } }

        /// <summary>
        /// Input variable is Solver.MkVar(0, InputSort).
        /// </summary>
        public TERM InputVar { get { return solver.MkVar(0, inputSort); } }

        /// <summary>
        /// inernal version that works correctly with a product construction
        /// </summary>
        internal ST<FUNC, TERM, SORT> ExploreFull()
        {
            var stack = new Stack<Tuple<int, TERM>>();
            var stateIdMap = new Dictionary<Tuple<int, TERM>, int>();
            var initState = new Tuple<int, TERM>(automaton.InitialState, initReg);
            stack.Push(new Tuple<int, TERM>(automaton.InitialState, initReg));
            stateIdMap[initState] = 0;
            int nextStateId = 1;

            var moves = new List<Move<Rule<TERM>>>();

            while (stack.Count > 0)
            {
                var pair = stack.Pop();
                int sourceState = stateIdMap[pair];
                foreach (var move in GetNonFinalMovesFrom(pair.Item1))
                {
                    TERM g = solver.Simplify(solver.ApplySubstitution(move.Label.Guard, RegisterVar, pair.Item2));
                    TERM u = solver.Simplify(solver.ApplySubstitution(move.Label.Update, RegisterVar, pair.Item2));
                    TERM[] ys = Array.ConvertAll(move.Label.Yields, y => solver.Simplify(solver.ApplySubstitution(y, RegisterVar, pair.Item2)));
                    TERM[] ys2 = (move.Label.Yields2 == null ? null :
                        Array.ConvertAll(move.Label.Yields2, y => solver.Simplify(solver.ApplySubstitution(y, RegisterVar, pair.Item2))));

                    foreach (var upd in FindDistinctRegisterValues(g, u))
                    {
                        var targetPair = new Tuple<int, TERM>(move.TargetState, upd);
                        int targetState;
                        if (!stateIdMap.TryGetValue(targetPair, out targetState))
                        {
                            targetState = nextStateId;
                            nextStateId += 1;
                            stateIdMap[targetPair] = targetState;
                            stack.Push(targetPair);
                        }
                        TERM pred = solver.MkAnd(g, solver.MkEq(u, upd));
                        moves.Add(Move<Rule<TERM>>.Create(sourceState, targetState, new Rule<TERM>(move.Label.k, pred, ys, ys2, solver.UnitConst)));
                    }
                }
                foreach (var rule in GetFinalRules(pair.Item1))
                {
                    TERM g = solver.ApplySubstitution(rule.Guard, RegisterVar, pair.Item2);
                    if (solver.MainSolver.GetModel(g) != null) //if the guard is satisfiable it is true because it is ground
                    {
                        var ys1 = Array.ConvertAll(rule.Yields, y => solver.ApplySubstitution(y, RegisterVar, pair.Item2));
                        var ys2 = (rule.Yields2 == null ? null :
                            Array.ConvertAll(rule.Yields2, y => solver.ApplySubstitution(y, RegisterVar, pair.Item2)));
                        moves.Add(Move<Rule<TERM>>.Create(sourceState, sourceState, new Rule<TERM>(0, solver.True, ys1, ys2, default(TERM))));
                    }
                }
            }

            var sft = Create(solver, string.Format("SFT[{0}]", name), solver.UnitConst, inputSort, outputSort, solver.UnitSort, 0, moves);
            sft.automaton.EliminateDeadStates();
            return sft;
        }

         /// <summary>
        /// Computes an equivalent ST by exploring all the Boolean register components, preserves all nonBoolean register components as symbolic.
        /// Is equivalent to Explore() when all register components are Boolean.
        /// </summary>
        public ST<FUNC, TERM, SORT> ExploreBools()
        {
            if (_STb != null)
                return (_STb.ExploreBools()).ToST();

            if (STbuilder.ContainsOnlyBooleans(registerSort))
                return this.ExploreAlgo(false, 0); //full exploration
            else if (!STbuilder.ContainsSomeBooleans(registerSort))
                return this;                    //there are no Booleans to explore
            else
                return ExploreAlgo(true, 0);       //use Boolean projection
        }

        
        /// <summary>
        /// Explore and group. Returns an equivalent (up to grouping) SFT.
        /// The result is multi-output if this ST is multi-output.
        /// Assumes that this ST has been lifted.
        /// </summary>
        public ST<FUNC, TERM, SORT> ExploreAndGroup()
        {
            if (!solver.IsOption(inputSort))
                throw new AutomataException(AutomataExceptionKind.STMustBeLifted);

            var stateMap = new Dictionary<int, Tuple<int, TERM>>();
            var stateIdMap = new Dictionary<Tuple<int, TERM>, int>();
            var q0 = Tuple.Create(this.InitialState, InitialRegister);
            stateMap[0] = q0;
            stateIdMap[q0] = 0; //0 is the inital state
            SimpleStack<int> Wnext = new SimpleStack<int>();
            SimpleStack<int> Wcurr = null;
            Wnext.Push(0);
            var sft_moves = new SimpleStack<Move<Rule<TERM>>>(); 
            int nextStateId = 1;
            var i_lifted = solver.MkVar(0, InputListSort);
            while (Wnext.IsNonempty)
            {
                Wcurr = Wnext;
                Wnext = new SimpleStack<int>();
                while (Wcurr.IsNonempty)
                {
                    var qS = Wcurr.Pop();
                    var q_r = stateMap[qS];
                    int q = q_r.Item1;
                    TERM r = q_r.Item2;
                    if (automaton.IsFinalState(q))
                    {
                        var frule = new Rule<TERM>(0, solver.True, new TERM[] { }, new TERM[] { }, default(TERM));
                        var fmove = Move<Rule<TERM>>.Create(qS, qS, frule);
                        sft_moves.Push(fmove);
                    }
                    else
                        foreach (var gv in Group(i_lifted, i_lifted, q, r, 1, solver.True, SimpleList<TERM>.Empty, SimpleList<TERM>.Empty))
                        {
                            var q1_r1 = Tuple.Create(gv.EndState, gv.Register);
                            int qE;
                            if (!stateIdMap.TryGetValue(q1_r1, out qE))
                            {
                                qE = nextStateId++;
                                stateMap[qE] = q1_r1;
                                stateIdMap[q1_r1] = qE;
                                Wnext.Push(qE);
                            }
                            var rule = new Rule<TERM>(1, gv.Guard, gv.Out1.ToArray(), gv.Out2.ToArray(), solver.UnitConst);
                            var move = Move<Rule<TERM>>.Create(qS, qE, rule);
                            sft_moves.Push(move);
                        }
                }
            }
            var sft = ST<FUNC,TERM,SORT>.Create(solver, "EG(" + this.name + ")", solver.UnitConst, this.InputListSort, this.outputSort, solver.UnitSort, 0, sft_moves);
            return sft;
        }

        private IEnumerable<GroupValue> Group(TERM i_lifted, TERM i_rest, int q, TERM r, int k, TERM phi, SimpleList<TERM> out1, SimpleList<TERM> out2)
        {
            ////there is a singlefinal state
            //if (automaton.IsFinalState(q))
            //{
            //    foreach (var frule in GetFinalRules(q))
            //    {
            //        var phiF = solver.MkAnd(phi, solver.ApplySubstitution(frule.Guard, RegisterVar, r));
            //        if (solver.IsSatisfiable(phiF))
            //        {
            //            var o1F = frule.Output.ConvertAll(v => solver.Simplify(solver.ApplySubstitution(v, RegisterVar, r)));
            //            var o2F = frule.Output2.ConvertAll(v => solver.Simplify(solver.ApplySubstitution(v, RegisterVar, r)));
            //            yield return new GroupValue(solver.True, out1.Append(o1F), out2.Append(o2F), k - 1, -1, default(TERM));
            //        }
            //    }
            //}
            //else
            //{
            foreach (var tr in GetNonFinalMovesFrom(q))
            {
                var i_first = solver.MkFirstOfList(i_rest);
                var theta = new Dictionary<TERM, TERM>();
                theta[InputVar] = i_first;
                theta[RegisterVar] = r;
                var phi1 = solver.Simplify(solver.MkAnd(phi, solver.MkAnd(solver.MkIsCons(i_rest), solver.ApplySubstitution(tr.Label.Guard, theta))));
                if (solver.IsSatisfiable(phi1))
                {
                    var o1 = tr.Label.Output.ConvertAll(y => solver.Simplify(solver.ApplySubstitution(y, theta)));
                    var o2 = tr.Label.Output2.ConvertAll(y => solver.Simplify(solver.ApplySubstitution(y, theta)));
                    var r1 = solver.Simplify(solver.ApplySubstitution(tr.Label.Update, theta));
                    var r1_fixed = GetUniqueValue(i_lifted, phi1, r1);
                    if (!object.Equals(r1_fixed, default(TERM)))
                    {
                        var phi2 = solver.Simplify(solver.MkAnd(phi1, solver.MkIsNil(solver.MkRestOfList(i_rest))));
                        yield return new GroupValue(phi2, out1.Append(o1), out2.Append(o2), k, tr.TargetState, r1_fixed);
                    }
                    else
                        foreach (var gv in Group(i_lifted, solver.MkRestOfList(i_rest), tr.TargetState, r1, k + 1, phi1, out1.Append(o1), out2.Append(o2)))
                            yield return gv;
                }
            }
            //}
        }

        private TERM GetUniqueValue(TERM i, TERM phi, TERM r)
        {
            solver.MainSolver.Push();
            TERM x1 = solver.MkFreshConst("x1", solver.GetSort(i));
            TERM x2 = solver.MkFreshConst("x2", solver.GetSort(i));
            TERM y1 = solver.MkFreshConst("y1", RegisterSort);
            TERM y2 = solver.MkFreshConst("y2", RegisterSort);
            var phi1 = solver.ApplySubstitution(phi, i, x1, RegisterVar, y1);
            var phi2 = solver.ApplySubstitution(phi, i, x2, RegisterVar, y2);
            var r1 = solver.ApplySubstitution(r, i, x1, RegisterVar, y1);
            var r2 = solver.ApplySubstitution(r, i, x2, RegisterVar, y2);
            var someModel = solver.MainSolver.GetModel(solver.MkAnd(phi1, solver.MkAnd(phi2, solver.MkNeq(r1, r2))));
            if (someModel != null)
            {
                solver.MainSolver.Pop();
                return default(TERM);
            }
            else
            {
                var m = solver.MainSolver.GetModel(phi1, r1);
                TERM r1val = m[r1].Value;
                solver.MainSolver.Pop();
                return r1val;
            }
        }

        private class GroupValue
        {
            internal TERM Guard;
            internal SimpleList<TERM> Out1;
            internal SimpleList<TERM> Out2;
            internal int Width;
            internal int EndState;
            internal TERM Register;
            internal GroupValue(TERM guard, SimpleList<TERM> out1, SimpleList<TERM> out2, int width, int endState, TERM register)
            {
                this.Guard = guard;
                this.Out1 = out1;
                this.Out2 = out2;
                this.Width = width;
                this.EndState = endState;
                this.Register = register;
            }
        }


        /// <summary>
        /// Computes an equivalent finite state symbolic transducer (if one exists) by exploring the registers using DFS.
        /// The computation does not terminate when the number of resulting states is infinite.
        /// The resulting ST does not use registers and therefore has register sort Solver.UnitSort, 
        /// initial register is Solver.UnitConst, and all (nonfinal) rules have register update Solver.UnitConst.
        /// </summary>
        public ST<FUNC, TERM, SORT> Explore()
        {
            if (_STb != null)
                return (_STb.Explore()).ToST();

            return ExploreAlgo(false, 0);
        }

        /// <summary>
        /// Explore for max nr of inputs.
        /// </summary>
        public ST<FUNC, TERM, SORT> Explore(int maxInputs)
        {
            return ExploreAlgo(false, maxInputs);
        }

        public Automaton<string> MkInstance()
        {
            if (!IsRegisterFree)
                throw new AutomataException(AutomataExceptionKind.NotRegisterFree);
            var new_moves = new List<Move<string>>();
            var new_fin_state = automaton.MaxState + 1;
            foreach (var move in automaton.GetMoves())
            {
                if (move.Label.IsFinal)
                {
                    string lab = "[";
                    foreach (var y in move.Label.Yields)
                    {
                        if (lab != "[")
                            lab += ",";
                        var t = solver.MainSolver.FindOneMember(solver.MkEq(solver.MkVar(0, outputSort), y)).Value;
                        char c = (char)solver.GetNumeralUInt(t);
                        lab += Automata.StringUtility.Escape(c);
                    }
                    lab += "]";
                    var lab_str = "/" + lab;
                    new_moves.Add(Move<string>.Create(move.SourceState, new_fin_state, lab_str));
                }
                else
                {
                    string lab = "[";
                    var a = (solver.IsGround(move.Label.guard) ? (int)'a' : (int)solver.GetNumeralUInt(solver.MainSolver.FindOneMember(move.Label.guard).Value));
                    var v = solver.MkNumeral(a, InputSort);
                    foreach (var y in move.Label.Yields)
                    {
                        if (lab != "[")
                            lab += ",";
                        var t = solver.MainSolver.FindOneMember(solver.MkEq(solver.MkVar(0, outputSort), (solver.ApplySubstitution(y, InputVar, v)))).Value;
                        char c = (char)solver.GetNumeralUInt(t);
                        lab += Automata.StringUtility.Escape(c);
                    }
                    lab += "]";
                    var lab_str = Automata.StringUtility.Escape((char)a) + "/" + lab;
                    new_moves.Add(Move<string>.Create(move.SourceState, move.TargetState, lab_str));
                }
            }
            var aut = Automaton<string>.Create(null, InitialState, new int[]{new_fin_state}, new_moves);
            return aut;
        }

        /// <summary>
        /// Main exploration algo
        /// </summary>
        ST<FUNC, TERM, SORT> ExploreAlgo(bool useBP, int max_nr_of_inputs)
        {

            //extract the boolean and enum-like registers and the nonboolean registers if useBP=true, 
            //otherwise perform full exploration

            TERM proj_concrete = RegisterVar;
            TERM proj_symbolic = solver.UnitConst;
            Func<TERM, TERM, TERM> combine = ((x1, x2) => x1);
            if (useBP)
                STbuilder.GetProjectionPair(this, out proj_concrete, out proj_symbolic, out combine);

            if (proj_concrete.Equals(solver.UnitConst))
                return this;

            //output abstraction functions
            Func<TERM, TERM> fBP = (t => solver.Simplify(solver.ApplySubstitution(proj_concrete, RegisterVar, t)));
            Func<TERM, TERM> fNBP = (t => solver.Simplify(solver.ApplySubstitution(proj_symbolic, RegisterVar, t)));

            TERM initBools = fBP(initReg);
            TERM initRest = fNBP(initReg);

            SORT bSort = Solver.GetSort(initBools);
            SORT newRegSort = Solver.GetSort(initRest);

            TERM b = Solver.MkVar(2, bSort);

            TERM newReg = STbuilder.MkRegister(newRegSort);
             
            var stack = new Stack<Tuple<int, TERM>>();
            var states = new Dictionary<Tuple<int, TERM>, int>();

            var initPair = new Tuple<int, TERM>(0, initBools);
            states[initPair] = 0;
            int stateCntr = 1;
            stack.Push(initPair);

            var nfMoveMap = new Dictionary<Tuple<Tuple<int, int>, Tuple<TERM, Seq>>, TERM>();
            var fMoveMap = new Dictionary<Tuple<int, Seq>, TERM>();

            while (stack.Count > 0)
            {
                var pair = stack.Pop();
                var sourceState = states[pair];
                //make an instance of the input register state wrt to the concrete part 

                var regInst = combine(pair.Item2, newReg);

                //var r00 = (useBP ? STbuilder.MkBPInstance(RegisterVar, b0, newReg) : b0);

                foreach (var move in GetNonFinalMovesFrom(pair.Item1))
                {
                    #region normal moves

                    var grdInst = Solver.ApplySubstitution(move.Label.Guard, RegisterVar, regInst);
                    var updInst = Solver.ApplySubstitution(move.Label.Update, RegisterVar, regInst);
                    var bp = fBP(updInst); // (useBP ? STbuilder.BP(updInst) : updInst);
                    var nbp = fNBP(updInst);
                    var cond = STbuilder.And(grdInst, Solver.MkEq(b, bp));

                    var bVals = new List<TERM>();

                    #region find all possible (or up to max_nr_of_inputs if max_nr_of_inputs>0) of b values
                    solver.MainSolver.Push();
                    var b_tmp = solver.MkFreshConst("b_tmp", bSort);
                    var c_tmp = solver.MkFreshConst("c_tmp", inputSort);
                    var r_tmp = solver.MkFreshConst("r_tmp", newRegSort);
                    var subst = new Dictionary<TERM,TERM>();
                    subst[b] = b_tmp;
                    subst[newReg] = r_tmp;
                    subst[InputVar] = c_tmp;
                    int __cnt__ = 0;

                    var assertion = Solver.ApplySubstitution(cond, subst);
                    var model = solver.MainSolver.GetModel(assertion, b_tmp);
                    while (model != null)
                    {
                        __cnt__ += 1;
                        var bVal = model[b_tmp].Value;
                        bVals.Add(bVal);
                        assertion = Solver.MkAnd(assertion, Solver.MkNeq(b_tmp, bVal));
                        model = (max_nr_of_inputs <= 0 || __cnt__ <= max_nr_of_inputs ? solver.MainSolver.GetModel(assertion, b_tmp) : null);
                    }
                    solver.MainSolver.Pop();
                    #endregion

                    //when useBP is true, bVals are all the potentially reachable boolean states (a boolean abstraction)
                    foreach (var bVal in bVals)
                    {
                        var newPair = new Tuple<int, TERM>(move.TargetState, bVal);
                        int targetState;
                        if (!states.TryGetValue(newPair, out targetState))
                        {
                            targetState = stateCntr;
                            stateCntr += 1;
                            states[newPair] = targetState;
                            stack.Push(newPair);
                        }
                        var guard = solver.ToNNF(solver.Simplify(solver.ApplySubstitution(cond, b, bVal)));
                        var update = nbp;
                        var yields = Array.ConvertAll(move.Label.Yields, y => solver.ToNNF(solver.Simplify(solver.ApplySubstitution(y, RegisterVar, regInst))));

                        var key = new Tuple<Tuple<int, int>, Tuple<TERM, Seq>>(new Tuple<int, int>(sourceState, targetState), new Tuple<TERM, Seq>(update, new Seq(yields)));
                        TERM combinedGuard;
                        if (nfMoveMap.TryGetValue(key, out combinedGuard))
                        {
                            combinedGuard = STbuilder.Or(combinedGuard, guard);
                            if (!combinedGuard.Equals(Solver.True) && !Solver.IsSatisfiable(Solver.MkNot(combinedGuard)))
                                combinedGuard = Solver.True;
                        }
                        else
                        {
                            combinedGuard = guard;
                        }
                        nfMoveMap[key] = combinedGuard;
                    }

                    #endregion
                }

                foreach (var rule in GetFinalRules(pair.Item1))
                {
                    #region final rules
                    var cond = Solver.ApplySubstitution(rule.Guard, RegisterVar, regInst);
                    if (Solver.IsSatisfiable(cond))
                    {
                        var yields = Array.ConvertAll(rule.Yields, y => solver.ToNNF(solver.Simplify(solver.ApplySubstitution(y, RegisterVar, regInst))));
                        var key = new Tuple<int, Seq>(sourceState, new Seq(yields));
                        var guard = solver.ToNNF(solver.Simplify(solver.ApplySubstitution(rule.Guard, RegisterVar, regInst)));
                        TERM combinedGuard;
                        if (fMoveMap.TryGetValue(key, out combinedGuard))
                        {
                            combinedGuard = STbuilder.Or(combinedGuard, guard);
                            if (!combinedGuard.Equals(Solver.True) && !Solver.IsSatisfiable(Solver.MkNot(combinedGuard)))
                                combinedGuard = Solver.True;
                        }
                        else
                        {
                            combinedGuard = guard;
                        }
                        fMoveMap[key] = combinedGuard;
                    }
                    #endregion
                }
            }

            var moves = new List<Move<Rule<TERM>>>();
            foreach (var entry in nfMoveMap)
            {
                moves.Add(STbuilder.MkRule(entry.Key.Item1.Item1, entry.Key.Item1.Item2, entry.Value, entry.Key.Item2.Item1, entry.Key.Item2.Item2.elems));
            }
            foreach (var entry in fMoveMap)
            {
                moves.Add(STbuilder.MkFinalOutput(entry.Key.Item1, entry.Value, entry.Key.Item2.elems));
            }
            var st = STbuilder.MkST(string.Format("{1}E[{0}]", name, (useBP ? "B" : "F")), initRest, inputSort, outputSort, newRegSort, 0, moves);
            if (st.automaton.IsEmpty)
                return STbuilder.MkST(string.Format("{1}E[{0}]", name, (useBP ? "B" : "F")), initRest, inputSort, outputSort, newRegSort, 0, new Move<Rule<TERM>>[] { });
            else
            {
                st.automaton.EliminateDeadStates();
                st.isMultiInput = this.isMultiInput;
                return st;
            }
        }

        public bool AreGuardsRegisterFree()
        {
            foreach (var move in GetMoves())
            {
                foreach (var v in solver.GetVars(move.Label.Guard))
                    if (v.Equals(this.RegisterVar))
                        return false;
            }
            return true;
        }

        /// <summary>
        /// Extract the domain as an SFA.
        /// Returns null when an SFA conversion is not possible in the case of multi-input STs.
        /// </summary>
        public SFA<FUNC, TERM, SORT> ToSFA()
        {
            var current = this;

            current = ExploreAlgo(true, 0); //first eliminate Boolean registers, if any

            //current.ShowGraph(20);

            if (current.IsRegisterFree)
            {
                if (!current.isMultiInput)
                    return current.SFTtoSFA(); //the case of an SFT
                else
                    return current.MkCartesianExpansion();
            }

            var current1 = current.DeleteRegister();
            if (current1 != null)
                current = current1;
            else
            {
                var lifted = current.LiftIputSortToList();
                lifted.EliminateIntermediateStates();
                lifted.Clean();
                var lifted1 = lifted.DeleteRegister();
                if (lifted1 != null)
                {
                    var cartesian = lifted1.MkCartesianExpansion();
                    if (cartesian != null)
                        return cartesian;
                }
            }

            var st_B = (current.IsRegisterFree ? current : ExploreAlgo(true, 0));
            var moves = new List<Move<TERM>>();
            var finalStates = new List<int>();
            //check if no guard depends on the register, then registers must not be explored
            var st = (st_B.AreGuardsRegisterFree() ? st_B : st_B.ExploreAlgo(false, 0));
            foreach (var move in st.GetMoves())
                if (!move.Label.IsFinal)
                    moves.Add(Move<TERM>.Create(move.SourceState, move.TargetState, move.Label.Guard));
                else
                    finalStates.Add(move.TargetState);
            var sfa = new SFA<FUNC, TERM, SORT>(solver, inputSort, string.Format("SFA[{0}]", name),
                Automaton<TERM>.Create(this.solver, automaton.InitialState, finalStates, moves));
            return sfa;

        }

        /// <summary>
        /// Converts all yields to just output the given input.
        /// </summary>
        public ST<FUNC, TERM, SORT> ToIdOutput()
        {
            var st = this;
            var moves = new List<Move<Rule<TERM>>>();
            foreach (var move in st.GetMoves())
                if (!move.Label.IsFinal)
                    moves.Add(Move<Rule<TERM>>.Create(move.SourceState, move.TargetState, 
                        Rule<TERM>.Mk(move.Label.guard, move.Label.update, InputVar)));
                else
                    moves.Add(Move<Rule<TERM>>.Create(move.SourceState, move.TargetState,
                        Rule<TERM>.MkFinal(move.Label.guard)));
            var st1 = ST<FUNC, TERM, SORT>.Create(this.solver, this.name, this.initReg, this.inputSort, this.outputSort, this.registerSort,
                this.InitialState, moves);
            return st1;
        }

        public void Clean()
        {
            //compute reachable states
            HashSet<int> reachable = new HashSet<int>();
            Stack<int> stack = new Stack<int>();
            reachable.Add(InitialState);
            stack.Push(InitialState);
            while (stack.Count > 0)
            {
                var p = stack.Pop();
                foreach (var move in automaton.GetMovesFrom(p))
                    if (reachable.Add(move.TargetState))
                        stack.Push(move.TargetState);
            }

            var alive = new HashSet<int>(automaton.GetFinalStates());
            alive.IntersectWith(reachable);
            if (alive.Count == 0)
            {
                this.automaton = Automaton<Rule<TERM>>.MkEmpty(null);
                return;
            }
            //backwards reachability
            foreach (var st in alive)
                stack.Push(st);
            while (stack.Count > 0)
            {
                var p = stack.Pop();
                foreach (var move in automaton.GetMovesTo(p))
                    if (alive.Add(move.SourceState))
                        stack.Push(move.SourceState);
            }
            var waste = new HashSet<int>(automaton.GetStates());
            waste.RemoveWhere(x => (alive.Contains(x) && reachable.Contains(x)));
            foreach (var unreachable_or_deadend in waste)
                automaton.RemoveTheState(unreachable_or_deadend);
        }

        void MkEmpty()
        {
        }

        private SFA<FUNC, TERM, SORT> SFTtoSFA()
        {
            var moves = new List<Move<TERM>>();
            var finalStates = new List<int>();
            foreach (var move in GetMoves())
                if (!move.Label.IsFinal)
                    moves.Add(Move<TERM>.Create(move.SourceState, move.TargetState, move.Label.Guard));
                else
                    finalStates.Add(move.TargetState);
            var sfa = new SFA<FUNC, TERM, SORT>(solver, inputSort, string.Format("SFA[{0}]", name),
                Automaton<TERM>.Create(this.solver, automaton.InitialState, finalStates, moves));
            return sfa;
        }

        private SFA<FUNC, TERM, SORT> MkCartesianExpansion()
        {
            //try to expand all multi-rules to single rules
            var newStateId = automaton.MaxState + 1;
            var elemSort = solver.GetElemSort(inputSort);
            var c = solver.MkVar(0,elemSort);

            Func<int,TERM[], TERM> MkSlice = (i,w) => 
            {
                TERM[] listElems = new TERM[w.Length];
                for (int j=0; j<w.Length; j++)
                    listElems[j] = (i ==j ? c : w[j]);
                var slice = solver.MkList(listElems);
                return slice;
            };

            List<Move<TERM>> newMoves = new List<Move<TERM>>();

            foreach (var move in GetMoves())
            {
                var k = move.Label.k;
                if (k == 0)
                    continue;

                if (k == 1)
                {
                    var newMove = Move<TERM>.Create(move.SourceState, move.TargetState,
                        solver.Simplify(solver.ApplySubstitution(move.Label.guard, InputVar, solver.MkList(c))));
                    newMoves.Add(newMove);
                }
                else
                {
                    TERM[] slices = new TERM[k];
                    TERM[] projections = new TERM[k];

                    TERM[] witness = new TERM[k];

                    #region find a witness
                    solver.MainSolver.Push();
                    TERM[] elemConsts = new TERM[k];
                    var inpConst = solver.MkFreshConst("tmp_list", inputSort);
                    for (int i = 0; i < k; i++)
                        elemConsts[i] = solver.MkFreshConst("tmp_elem", elemSort);
                    var witness_pred = solver.MkAnd(solver.ApplySubstitution(move.Label.guard, InputVar, inpConst),
                                                    solver.MkEq(inpConst, solver.MkList(elemConsts)));
                    var model = solver.MainSolver.GetModel(witness_pred, elemConsts);
                    for (int i = 0; i < k; i++)
                        witness[i] = model[elemConsts[i]].Value;
                    solver.MainSolver.Pop();
                    #endregion

                    for (int i = 0; i < k; i++)
                    {
                        slices[i] = solver.Simplify(solver.ApplySubstitution(move.Label.guard, InputVar, MkSlice(i, witness)));
                        projections[i] = solver.Simplify(solver.ApplySubstitution(slices[i], c, solver.MkFirstOfList(KthRest(i, InputVar))));
                    }
                    var pred = solver.MkAnd(MkListHasLength(InputVar, k), solver.MkNeq(move.Label.guard, solver.MkAnd(projections)));

                    if (solver.IsSatisfiable(pred))
                    {
                        //string w = solver.FindOneMember(pred).StringValue;
                        //Console.WriteLine(w);
                        return null; //cartesian expansion does not exist
                    }

                    int q = move.SourceState;
                    for (int i = 0; i < k; i++)
                    {
                        var newMove = Move<TERM>.Create(q, (i < k-1 ? newStateId++ : move.TargetState), solver.Simplify(slices[i]));
                        newMoves.Add(newMove);
                        q = newMove.TargetState;
                    }
                }
            }

            var aut = Automaton<TERM>.Create(this.solver, automaton.InitialState, automaton.GetFinalStates(), newMoves);
            return new SFA<FUNC, TERM, SORT>(solver, elemSort, aut);
        }

        TERM MkListHasLength(TERM list, int k)
        {
            TERM pred = solver.MkIsNil(KthRest(k, list));
            for (int i = 0; i < k; i++)
                pred = solver.MkAnd(pred, solver.MkIsCons(KthRest(i, list)));
            return pred;
        }

        IEnumerable<TERM> FindDistinctRegisterValues(TERM guard, TERM update)
        {
            if (IsGround(update))
            {
                if (solver.IsSatisfiable(guard))
                    yield return update;
            }
            else
            {
                List<TERM> res = new List<TERM>();
                List<TERM> inps = new List<TERM>();

                solver.MainSolver.Push(); //create a temporary context in the solver during search of all solutions

                TERM inp = solver.MkFreshConst("inp", inputSort);
                TERM upd = solver.MkFreshConst("upd", registerSort);
                TERM pred = solver.MkAnd(solver.ApplySubstitution(guard, InputVar, inp),
                                         solver.MkEq(solver.ApplySubstitution(update, InputVar, inp), upd));

                var model = solver.MainSolver.GetModel(pred, upd, inp);

                while (model != null) //when model==null then all solutions have been found
                {
                    var updVal = model[upd].Value; 

                    res.Add(updVal);           // save the update value
                    inps.Add(model[inp].Value);// save also the input value responsible for the update value

                    solver.MainSolver.Pop(); //remove the temporary context

                    yield return updVal;

                    solver.MainSolver.Push(); //recreate a temporary context in the solver for search of further solutions

                    inp = solver.MkFreshConst("inp", inputSort);
                    upd = solver.MkFreshConst("upd", registerSort);

                    List<TERM> cases = new List<TERM>();
                    cases.Add(solver.MkEq(solver.ApplySubstitution(update, InputVar, inp), upd));

                    for (int i = 0; i < res.Count; i++)
                        cases.Add(solver.MkAnd(solver.MkNeq(upd, res[i]), solver.MkNeq(inp, inps[i])));

                    pred = solver.MkAnd(solver.ApplySubstitution(guard, InputVar, inp), solver.MkAnd(cases));
                    model = solver.MainSolver.GetModel(pred, upd, inp);
                }
                solver.MainSolver.Pop(); //all solutions have been found, so pop the temporary context
            }
        }

        bool IsGround(TERM t)
        {
            foreach (var v in solver.GetVars(t))
                return false;
            return true;
        }

        #endregion

        #region INameProvider Members

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new AutomataException(AutomataExceptionKind.InvalidAutomatonName);
                name = value;
                if (_STb != null)
                    _STb.Name = value;
            }
        }

        #endregion

        #region ITransducer<Rule<TERM>> Members

        /// <summary>
        /// Returns rule.IsFinal
        /// </summary>
        public bool IsFinalRule(Rule<TERM> rule)
        {
            return rule.IsFinal;
        }

        public string DescribeGuard(Rule<TERM> rule)
        {
            var theta = new Dictionary<TERM, TERM>();
            theta[InputVar] = solver.MkConst(__input_variable, inputSort);
            theta[RegisterVar] = solver.MkConst(__register_variable, registerSort);
            var u = GetUniqueValue(InputVar, rule.Guard, InputVar);
            if (u != null)
            {
                string x_u = Solver.PrettyPrint(solver.MkEq(InputVar,u), LookupVarName);
                return x_u;
            }
            //try to convert the guard into a BDD first
            BDD set = Solver.ConvertToCharSet(Solver.CharSetProvider, rule.Guard);
            if (set != null)
            {
                string res = Solver.CharSetProvider.PrettyPrint(set);
                return res;
            }
            string str = Solver.PrettyPrint(solver.ApplySubstitution(rule.Guard, theta));
            return str;
        }

        public string DescribeYields(Rule<TERM> rule)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            for (int i = 0; i < rule.Yields.Length; i++)
            {
                sb.Append(solver.PrettyPrint(rule.Yields[i], LookupVarName));
                if (i < rule.Yields.Length - 1)
                    sb.Append(",");
            }
            sb.Append(")");
            return sb.ToString();
        }

        public string DescribeUpdate(Rule<TERM> rule)
        {
            if (rule.IsFinal)
                return "";

            if (rule.Update.Equals(solver.UnitConst))
                return "";
            else
                return string.Format("{0}:={1}", __register_variable, solver.PrettyPrint(rule.Update, LookupVarName));
        }

        public bool IsGuardTrue(Rule<TERM> rule)
        {
            return object.Equals(solver.True, rule.Guard);
        }

        public int GetYieldsLength(Rule<TERM> rule)
        {
            return rule.Yields.Length;
        }

        public string DescribeStartLabel()
        {
            if (initReg.Equals(solver.UnitConst))
                return "";
            else
                return string.Format("{0}:={1}", __register_variable, solver.PrettyPrint(initReg));
        }

        #endregion

        #region 1-disequality

        /// <summary>
        /// Returns null if this ST and the second ST produce the same outputs for all inputs up to the given maximum length.
        /// Otherwise returns a counterexample to 1-equality of this ST and the second ST.
        /// </summary>
        /// <param name="st">the second ST</param>
        /// <param name="maxInputLength">maximum bound for the input sequences to be considered</param>
        public ICounterexample<TERM> Diff(ST<FUNC, TERM, SORT> st, int maxInputLength)
        {
            int inputLength; 
            IValue<TERM> input; 
            IValue<TERM> outputA; 
            IValue<TERM> outputB;
            bool diff = Witness1disequality(this, st, maxInputLength, out  inputLength, out  input, out  outputA, out  outputB);
            if (diff)
                return new CounterExample(inputLength, input, outputA, outputB);
            else
                return null;
        }

        /// <summary>
        /// Returns a counterexample to 1-equality of this ST and the second ST.
        /// When the STs are 1-equivalent, Diff may not terminate or returns null.
        /// Note: this.Diff(this) returns a counterexample iff this ST is not single-valued.
        /// </summary>
        /// <param name="st">the second ST</param>
        public ICounterexample<TERM> Diff(ST<FUNC, TERM, SORT> st)
        {
            int inputLength;
            IValue<TERM> input;
            IValue<TERM> outputA;
            IValue<TERM> outputB;
            bool diff = Witness1disequality(this, st, -1, out  inputLength, out  input, out  outputA, out  outputB);
            if (diff)
                return new CounterExample(inputLength, input, outputA, outputB);
            else
                return null;
        }

        /// <summary>
        /// Returns false iff this and B are not 1-equal, i.e., 
        /// if there exists u,v,w s.t. this(u,v) and B(u,w) but v!=w.
        /// Note that this.Eq1(this)=false iff this is not single-valued.
        /// </summary>
        public bool Eq1(ST<FUNC, TERM, SORT> st)
        {
            return Eq1(this, st);
        }

        public bool Eq1_OLD(ST<FUNC, TERM, SORT> st)
        {
            return Eq1_OLD(this, st);
        }


        static public bool Witness1disequality(ST<FUNC, TERM, SORT> A, ST<FUNC, TERM, SORT> B, int bound, out int inputLength, out IValue<TERM> input, out IValue<TERM> outputA, out IValue<TERM> outputB)
        {
            IContext<FUNC, TERM, SORT> z3p = A.Solver;
            if (B.Solver != z3p)
                throw new AutomataException(AutomataExceptionKind.SolversAreNotIdentical);

            if (!A.InputSort.Equals(B.InputSort) || !A.OutputSort.Equals(B.OutputSort))
                throw new AutomataException(AutomataExceptionKind.SortMismatch);

            A.AssertTheory();
            B.AssertTheory();

            z3p.MainSolver.Push();
            TERM i = z3p.GetNil(A.InputListSort);
            TERM a = z3p.MkFreshConst("out_" + A.Name, A.OutputListSort);
            TERM b = z3p.MkFreshConst("out_" + B.Name, A.OutputListSort);

            int k = 0;
            IDictionary<TERM, IValue<TERM>> model = null;

            while (bound < 0 || k <= bound)
            {
                TERM assertion = z3p.MkAnd(A.MkAccept(i, a), z3p.MkAnd(B.MkAccept(i, b), z3p.MkNeq(a, b)));
                model = z3p.MainSolver.GetModel(assertion, i, a, b);
                if (model == null)
                {
                    i = z3p.MkListCons(z3p.MkFreshConst("elem_" + k, A.InputSort), i);
                    k += 1;
                }
                else
                {
                    inputLength = k;
                    input = model[i];
                    outputA = model[a];
                    outputB = model[b];
                    z3p.MainSolver.Pop();
                    return true;
                }
            }
            z3p.MainSolver.Pop();
            inputLength = -1;
            input = null;
            outputA = null;
            outputB = null;
            return false;
        }

        static CounterExample WitnessNeq1(ST<FUNC, TERM, SORT> AxB)
        {

            AxB.AssertProdTheory();
            var z3p = AxB.Solver;

            z3p.MainSolver.Push();
            TERM a = z3p.MkFreshConst("out1", AxB.OutputListSort);
            TERM b = z3p.MkFreshConst("out2", AxB.OutputListSort);
            TERM i = z3p.GetNil(AxB.InputListSort);
            int k = 0;
            while (true)
            {
                z3p.MainSolver.Push();
                IDictionary<TERM, IValue<TERM>> model = null;
                TERM assertion = z3p.MkAnd(AxB.MkProdAccept(i, a, b), z3p.MkNeq(a, b));
                model = z3p.MainSolver.GetModel(assertion, i, a, b);
                z3p.MainSolver.Pop();
                if (model != null)
                {
                    z3p.MainSolver.Pop();
                    return new CounterExample(k, model[i], model[a], model[b]);
                }

                k += 1;
                i = z3p.MkListCons(z3p.MkFreshConst("e", AxB.InputSort), i);
            }
        }

        #endregion

        #region 1-equivalence algorithm 
        /// <summary>
        /// Returns false iff A and B are not 1-equal, i.e., 
        /// if there exists u,v,w s.t. A(u,v) and B(u,w) but v!=w.
        /// Note that Eq1(A,A)=false iff A is not single-valued.
        /// </summary>
        public static bool Eq1(ST<FUNC, TERM, SORT> A, ST<FUNC, TERM, SORT> B) 
        {
            if (!object.Equals(A.solver, B.solver))
                throw new AutomataException(AutomataExceptionKind.SolversAreNotIdentical);

            if (!object.Equals(A.inputSort, B.inputSort) || !object.Equals(A.outputSort, B.outputSort))
                throw new AutomataException(AutomataExceptionKind.SortMismatch);

            if (A.automaton.IsEmpty || B.automaton.IsEmpty)
                return true;

            var solver = A.solver;
            var ftb = A.solver;
            //the register sort of all rules in AxB is MkTupleSort(A.RegisterSort,B.RegisterSort)
            var AxB = Automaton<Rule<TERM>>.MkProduct_(A.automaton, B.automaton, new STProductSolver(solver, A.registerSort, B.registerSort));
            if (AxB.IsEmpty) //there are no inputs in common
                return true;

            AxB.isEpsilonFree = true;

            var stAxB = new ST<FUNC, TERM, SORT>(solver, string.Format("{0}x{1}", A.name, B.name),
                solver.MkTuple(A.initReg, B.initReg), A.inputSort, A.outputSort, solver.MkTupleSort(A.registerSort, B.registerSort),
                AxB, false);

            var stAxB_lifted = stAxB.LiftIputSortToList();
            stAxB_lifted.EliminateIntermediateStates();
            stAxB_lifted.UpdateMaxLookahead();
            //Console.WriteLine("Lookahead: {0}", stAxB_lifted.maxLookahead);
            var stAxB_simplified = stAxB_lifted.DeleteRegister();

            //if (stAxB_simplified != null)
            //{
            //    stAxB_simplified.ShowGraph(1);
            //    var testSFA = stAxB_simplified.MkCartesianExpansion().Determinize().Minimize();
            //    testSFA.ShowGraph(10);
            //}

            //create the SFT of the product, OBS!:this might not terminate
            var AB = (stAxB_simplified != null ? stAxB_simplified : stAxB_lifted.ExploreFull());
            if (AB.automaton.IsEmpty)
                return true;

            var z3p = solver;
            var _i = ftb.MkVar(0,AB.InputSort);

            var Q = new Dictionary<int, Tuple<Seq,Seq>>(); //promises

            Stack<int> stack = new Stack<int>();

            Q[AB.InitialState] = new Tuple<Seq, Seq>(Seq.Empty, Seq.Empty); //the initial promise is the pair of empty sequences

            stack.Push(AB.InitialState);
            HashSet<int> visited = new HashSet<int>();
            visited.Add(AB.InitialState);

            #region helper methods
            Func<int, Seq, Seq, bool> AddToSearch = (p, a, b) =>
            {
                if (visited.Add(p))
                {
                    stack.Push(p);
                    Q[p] = new Tuple<Seq,Seq>(a, b);
                    return true;
                }
                return false;
            };
            #endregion

            //search for invalid common outputs
            while (stack.Count > 0)
            {
                var curr = stack.Pop();
                foreach (var move in AB.automaton.GetMovesFrom(curr))
                {
                    var a = Q[curr].Item1;  //the promise from A
                    var b = Q[curr].Item2; //the promise from B
                    if (move.Label.IsFinal)
                    {
                        if (!a.Concat(move.Label.Yields).Equals(b.Concat(move.Label.Yields2)))
                            return false; //there is a conflict in final outputs
                    }
                    else
                    {
                        TERM[] y1 = new TERM[move.Label.Yields.Length + a.Length];
                        TERM[] y2 = new TERM[move.Label.Yields2.Length + b.Length];
                        for (int i = 0; i < y1.Length; i++)
                            y1[i] = (i < a.Length ? a[i] : move.Label.Yields[i - a.Length]);
                        for (int i = 0; i < y2.Length; i++)
                            y2[i] = (i < b.Length ? b[i] : move.Label.Yields2[i - b.Length]);

                        int m = Math.Min(y1.Length, y2.Length);

                        #region check that the outputs coincide upto m for all inputs
                        TERM[] yieldDisequalities = new TERM[m];
                        for (int i = 0; i < m; i++)
                        {
                            yieldDisequalities[i] = z3p.MkNeq(y1[i], y2[i]);
                        }
                        TERM assert = z3p.MkAnd(move.Label.Guard, z3p.MkOr(yieldDisequalities));
                        if (z3p.IsSatisfiable(assert))
                        {
                            return false; //NOT EQUIVALENT, input c can yield different outputs
                        }
                        #endregion

                        //check the rests
                        TERM[] y1rest = new TERM[y1.Length - m];
                        TERM[] y2rest = new TERM[y2.Length - m];
                        for (int i = 0; i < y1rest.Length; i++)
                            y1rest[i] = y1[m + i];
                        for (int i = 0; i < y2rest.Length; i++)
                            y2rest[i] = y2[m + i];

                        if (y1rest.Length > 0) //y2rest.Length == 0
                        {
                            //check if there exist two inputs that can result in different outputs 
                            //TERM i1;
                            //TERM i2;
                            if (CheckIfTwoDistinguishingInputsExist(ftb, move.Label.Guard, y1rest, _i /*, out i1, out i2 */))
                            {
                                return false; // distinct pending outputs exist
                            }
                            else //we know that at least one input is possible
                            {
                                var i1 = GetOneMember(_i, ftb, move);
                                var s = new Seq(Array.ConvertAll(y1rest, y => ftb.Simplify(z3p.ApplySubstitution(y, _i, i1))));
                                bool added = AddToSearch(move.TargetState, s, Seq.Empty);
                                if (!added && !Q[move.TargetState].Equals(new Tuple<Seq,Seq>(s, Seq.Empty)))
                                    return false; // distinct pending outputs exist
                            }
                        }
                        else //y1rest.Length == 0 and y2rest.Length > 0
                        {
                            //check if there exist two input chars that can result in different outputs 
                            //TERM i1;
                            //TERM i2;
                            if (CheckIfTwoDistinguishingInputsExist(ftb, move.Label.Guard, y2rest, _i /*, out i1, out i2*/))
                            {
                                return false; // distinct pending outputs
                            }
                            else //we know that at least one input is possible
                            {

                                var i1 = GetOneMember(_i, ftb, move);
                                var s = new Seq(Array.ConvertAll(y1rest, y => ftb.Simplify(z3p.ApplySubstitution(y, _i, i1))));
                                bool added = AddToSearch(move.TargetState, Seq.Empty, s);
                                if (!added && !Q[move.TargetState].Equals(new Tuple<Seq, Seq>(Seq.Empty,s)))
                                    return false; // distinct pending outputs
                            }
                        }
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// Returns null if this ST and the other ST are 1-equal, returns a conterexample otherwise.
        /// </summary>
        public Sequence<TERM> Neq1(ST<FUNC, TERM, SORT> other)
        {
            return Neq1(this, other);
        }

        /// <summary>
        /// Returns null if A and B are 1-equal, returns a counterexample otherwise.
        /// </summary>
        public static Sequence<TERM> Neq1(ST<FUNC, TERM, SORT> A, ST<FUNC, TERM, SORT> B)
        {
            if (!object.Equals(A.solver, B.solver))
                throw new AutomataException(AutomataExceptionKind.SolversAreNotIdentical);

            if (!object.Equals(A.inputSort, B.inputSort) || !object.Equals(A.outputSort, B.outputSort))
                throw new AutomataException(AutomataExceptionKind.SortMismatch);

            if (A.automaton.IsEmpty || B.automaton.IsEmpty)
                return null;

            var solver = A.solver;
            var ftb = A.solver;
            //the register sort of all rules in AxB is MkTupleSort(A.RegisterSort,B.RegisterSort)
            var AxB = Automaton<Rule<TERM>>.MkProduct_(A.automaton, B.automaton,new STProductSolver(solver, A.registerSort, B.registerSort));
            if (AxB.IsEmpty) //there are no inputs in common
                return null;

            AxB.isEpsilonFree = true;

            var C = new ST<FUNC, TERM, SORT>(solver, string.Format("{0}x{1}", A.name, B.name),
                solver.MkTuple(A.initReg, B.initReg), A.inputSort, A.outputSort, solver.MkTupleSort(A.registerSort, B.registerSort),
                AxB, false);


            var AB = C.AddEOI().ExploreAndGroup();

            var z3p = solver;
            var _i = ftb.MkVar(0, AB.InputSort);

            Func<TERM, IValue<TERM>> FindOneMember = pred =>
                {
                    var mem = solver.MainSolver.FindOneMember(pred);
                    return mem;
                };

            Func<IEnumerable<TERM>, Sequence<TERM>> GetWitness = preds =>
            {
                List<TERM> witness = new List<TERM>();
                foreach (var pred in preds)
                    witness.AddRange(FindOneMember(pred).GetList());
                return new Sequence<TERM>(witness.ToArray());
            };

            Func<IEnumerable<TERM>, Sequence<TERM>> UnliftVals = vs =>
                {
                    return new Sequence<TERM>(AB.UnliftValues(vs));
                };

            #region first: decide if there is a length conflict
            var path = new Dictionary<int, Tuple<SimpleList<TERM>,int,int>>();
            var nextLevel = new Stack<int>();
            path[AB.InitialState] = Tuple.Create(SimpleList<TERM>.Empty, 0, 0);
            nextLevel.Push(AB.InitialState);
            while (nextLevel.Count > 0)
            {
                var currentLevel = nextLevel;
                nextLevel = new Stack<int>();
                while (currentLevel.Count > 0)
                {
                    var q = currentLevel.Pop();
                    var q_path = path[q];
                    var q_offset = q_path.Item2 - q_path.Item3;
                    foreach (var trans in AB.GetFinalRules(q))
                    {
                        var diff = trans.Yields.Length - trans.Yields2.Length;
                        int final_offset = q_offset + diff;
                        if (final_offset != 0)
                        {
                            var witness = GetWitness(q_path.Item1);
                            var witness_unlifted = UnliftVals(witness);
                            return witness_unlifted;
                        }
                    }
                    foreach (var trans in AB.GetNonFinalMovesFrom(q))
                    {
                        int p = trans.TargetState;
                        int n1 = q_path.Item2 + trans.Label.Yields.Length;
                        int n2 = q_path.Item3 + trans.Label.Yields2.Length;
                        var p_offset = n1 - n2;
                        if (path.ContainsKey(p))
                        {
                            var p_path = path[p];
                            if (p_path.Item2 - p_path.Item3 != p_offset)
                            {
                                #region there is a length conflict, state p is reachable in two conflicting ways
                                //figure out which one is causing different outputs and select a witness
                                //find some path to a final state
                                var rest = AB.FindFinalPath_WithYieldLengths(p);
                                var l1_via_q = n1 + rest.Item2;
                                var l2_via_q = n2 + rest.Item3;
                                var l1 = p_path.Item2 + rest.Item2;
                                var l2 = p_path.Item3 + rest.Item3;
                                if ((l1_via_q == l2_via_q) && (l1 == l2))
                                    //this is impossible unless there is an internal implementation error
                                    throw new AutomataException(AutomataExceptionKind.InternalError);
                                var witness_preds = (l1 != l2 ? p_path.Item1 : q_path.Item1.Append(trans.Label.Guard)).Append(rest.Item1);
                                var witness_flat = new List<TERM>();
                                foreach (var pred in witness_preds)
                                    witness_flat.AddRange(FindOneMember(pred).GetList());
                                var witness_unlifted = UnliftVals(witness_flat);
                                return witness_unlifted;
                                #endregion
                            }
                        }
                        else
                        {
                            path[p] = Tuple.Create(q_path.Item1.Append(trans.Label.Guard), q_path.Item2 + trans.Label.Yields.Length, q_path.Item3 + trans.Label.Yields2.Length);
                            nextLevel.Push(p);
                        }
                    }
                }
            }

            #endregion

            //beyond this point we know that there are no length conflicts.

            #region second: decide if there is a position conflict

            var promise = new Dictionary<int, Tuple<SimpleList<TERM>, Sequence<TERM>, Sequence<TERM>>>();
            promise[AB.InitialState] = Tuple.Create(SimpleList<TERM>.Empty, Sequence<TERM>.Empty, Sequence<TERM>.Empty);
            nextLevel.Push(AB.InitialState);
            while (nextLevel.Count > 0)
            {
                var currentLevel = nextLevel;
                nextLevel = new Stack<int>();
                while (currentLevel.Count > 0)
                {
                    var q = currentLevel.Pop();
                    var q_prom = promise[q];
                    var q_a_prom = q_prom.Item2;  //the promise from A
                    var q_b_prom = q_prom.Item3;  //the promise from B
                    foreach (var frule in AB.GetFinalRules(q))
                        //asuming here that final rules have concrete values
                        if (!q_a_prom.Append(frule.Yields).Equals(q_b_prom.Append(frule.Yields2)))
                        {
                            var witness = GetWitness(q_prom.Item1); //there is a conflict in final outputs
                            var witness_unlifted = UnliftVals(witness);
                            return witness_unlifted;
                        }
                    foreach (var move in AB.GetNonFinalMovesFrom(q))
                    {
                        var p = move.TargetState;
                        var y1 = q_a_prom.Append(move.Label.Yields);
                        var y2 = q_b_prom.Append(move.Label.Yields2);
                        int m = Math.Min(y1.Length, y2.Length);

                        if (m > 0)
                        {
                            #region check that the outputs coincide upto m for all inputs
                            TERM[] yieldDisequalities = new TERM[m];
                            for (int i = 0; i < m; i++)
                                yieldDisequalities[i] = solver.MkNeq(y1[i], y2[i]);
                            TERM assert = solver.MkAnd(move.Label.Guard, solver.MkOr(yieldDisequalities));
                            var v = FindOneMember(assert);
                            if (v != null)
                            {
                                var rest = AB.FindFinalPath(move.TargetState);
                                var witness = GetWitness(q_prom.Item1).Append(v.GetList()).Append(GetWitness(rest));
                                var witness_unlifted = UnliftVals(witness);
                                return witness_unlifted;
                            }
                            #endregion
                        }

                        var p_a_suffix = y1.Suffix(m);
                        var p_b_suffix = y2.Suffix(m);

                        if (p_a_suffix.Length == p_b_suffix.Length)
                        {
                            #region both suffixes are empty
                            //if promise.ContainsKey(p) then the stored promises must be empty as well because there are no length conflicts
                            if (!promise.ContainsKey(p))
                            {
                                promise[p] = Tuple.Create(q_prom.Item1.Append(move.Label.Guard), Sequence<TERM>.Empty, Sequence<TERM>.Empty);
                                nextLevel.Push(p);
                            }
                            #endregion
                        }
                        else //we know that exactly one of the suffixes is empty
                        {
                            var nonepty_suffix = (p_a_suffix.IsEmpty ? p_b_suffix : p_a_suffix);
                            if (nonepty_suffix.IsEmpty)
                                throw new AutomataException(AutomataExceptionKind.InternalError);

                            var i1_i2 = GetDistinguishingInputs(ftb, move.Label.Guard, nonepty_suffix.ToArray(), _i);
                            if (i1_i2 != null)
                            {
                                #region distinguishing inputs on either A or B
                                var i1 = i1_i2.Item1;
                                var i2 = i1_i2.Item2;
                                var inp_suff = GetWitness(AB.FindFinalPath(p));
                                var inp_pref = GetWitness(q_prom.Item1);
                                var witness1 = inp_pref.Append(GetWitness(i1.GetList())).Append(inp_suff);
                                //one of the inputs must lead to a position conflict, figure out which
                                var out1 = A.Apply(witness1);
                                var out2 = B.Apply(witness1);
                                if (!out1.Equals(out2))
                                {
                                    var witness_unlifted = UnliftVals(witness1);
                                    return witness_unlifted;
                                }
                                else
                                {
                                    var witness2 = inp_pref.Append(GetWitness(i2.GetList())).Append(inp_suff);
                                    var witness_unlifted = UnliftVals(witness2);
                                    return witness_unlifted;
                                }
                                #endregion
                            }
                            else
                            {
                                #region there is a nonempty promise
                                var i_list = FindOneMember(move.Label.Guard);
                                var nonepty_prom = nonepty_suffix.ConvertAll(y => ftb.Simplify(z3p.ApplySubstitution(y, _i, i_list.Value)));
                                if (promise.ContainsKey(p))
                                {
                                    var p_prom = promise[p];
                                    var pending_prom = (p_a_suffix.IsEmpty ? p_prom.Item3 : p_prom.Item2);
                                    if (!pending_prom.Equals(nonepty_prom))
                                    {
                                        //there is a position conflict, figure out which of the inputs causes it
                                        //there are two possibilities
                                        var inp_suff = new Sequence<TERM>(GetWitness(AB.FindFinalPath(p)));
                                        var inp_pref1 = new Sequence<TERM>(GetWitness(p_prom.Item1));
                                        var witness1 = inp_pref1.Append(inp_suff);
                                        var out1 = new Sequence<TERM>(A.Apply(witness1));
                                        var out2 = new Sequence<TERM>(B.Apply(witness1));
                                        if (!out1.Equals(out2))
                                        {
                                            var witness_unlifted = UnliftVals(witness1);
                                            return witness_unlifted;
                                        }
                                        else //must be the other case
                                        {
                                            var inp_pref2 = new Sequence<TERM>(GetWitness(q_prom.Item1)).Append(i_list.GetList());
                                            var witness2 = inp_pref2.Append(inp_suff);
                                            var witness_unlifted = UnliftVals(witness2);
                                            return witness_unlifted;
                                        }
                                    }
                                }
                                else
                                {
                                    promise[p] = Tuple.Create(q_prom.Item1.Append(move.Label.Guard), nonepty_prom, Sequence<TERM>.Empty);
                                    nextLevel.Push(p);
                                }
                                #endregion
                            }
                        }
                    }
                }
            }

            #endregion

            return null; //A and B are 1-equal
        }

        private IEnumerable<TERM> UnliftValues(IEnumerable<TERM> vs)
        {
            var none = solver.MkNone(solver.GetElemSort(inputSort));
            foreach (var v in vs)
                if (!v.Equals(none))
                    yield return solver.MkGetSomeValue(v);
        }

        /// <summary>
        /// Lift the inputsort to Option(inputsort), None is used exclusively as the EOI symbol.
        /// All valid inputs terminate with None.
        /// </summary>
        public ST<FUNC,TERM,SORT> AddEOI()
        {
            int qSink = automaton.MaxState + 1;
            var lifted_moves = new List<Move<Rule<TERM>>>();
            var i_sort_lifted = solver.MkOptionSort(inputSort);
            var i_lifted = solver.MkVar(0, i_sort_lifted);
            var i_extracted = solver.MkGetSomeValue(i_lifted);

            //there is a single final state qSink with associated final move
            var finalRule = new Rule<TERM>(0, solver.True, new TERM[] { }, new TERM[] { }, default(TERM));
            var finalMove = new Move<Rule<TERM>>(qSink, qSink, finalRule);
            lifted_moves.Add(finalMove);

            foreach (var move in GetMoves())
            {
                if (move.Label.IsFinal)
                {
                    var frule = new Rule<TERM>(1, solver.MkAnd(move.Label.Guard, solver.MkIsNone(i_lifted)), move.Label.Yields, move.Label.Yields2, initReg);
                    var fmove = Move<Rule<TERM>>.Create(move.SourceState, qSink, frule);
                    lifted_moves.Add(fmove);
                }
                else
                {
                    var lifted_guard = (solver.MkAnd(solver.ApplySubstitution(move.Label.Guard, InputVar, i_extracted), solver.MkIsSome(i_lifted)));
                    var lifted_out1 = move.Label.Output.ConvertAll(v => (solver.ApplySubstitution(v, InputVar, i_extracted)));
                    var lifted_out2 = move.Label.Output2.ConvertAll(v => (solver.ApplySubstitution(v, InputVar, i_extracted)));
                    var lifted_update = (solver.ApplySubstitution(move.Label.Update, InputVar, i_extracted));
                    var lifted_rule = new Rule<TERM>(1, lifted_guard, lifted_out1.ToArray(), lifted_out2.ToArray(), lifted_update);
                    var lifted_move = Move<Rule<TERM>>.Create(move.SourceState, move.TargetState, lifted_rule);
                    lifted_moves.Add(lifted_move);
                }
            }
            var res = ST<FUNC, TERM, SORT>.Create(solver, "Lift(" + name + ")", initReg, i_sort_lifted, outputSort, registerSort, InitialState, lifted_moves);
            return res;
        }


        private Tuple<TERM[], int, int> FindFinalPath_WithYieldLengths(int pStart)
        {
            var path = automaton.FindShortestFinalPath(pStart);
            var preds = Array.ConvertAll(path.Item1, r => r.Guard);
            int qf = path.Item2;
            var frule = ChooseFinalRule(qf);
            int k1 = frule.Yields.Length;
            int k2 = frule.Yields2.Length;
            for (int i = 0; i < path.Item1.Length; i++)
            {
                k1 += path.Item1[i].Yields.Length;
                k2 += path.Item1[i].Yields2.Length;
            }
            var res = Tuple.Create(preds, k1, k2);
            return res;
        }

        private TERM[] FindFinalPath(int pStart)
        {
            var path = automaton.FindShortestFinalPath(pStart);
            var preds = Array.ConvertAll(path.Item1, r => r.Guard);
            return preds;
        }

        Rule<TERM> ChooseFinalRule(int q)
        {
            foreach (var rule in GetFinalRules(q))
                return rule;
            return null;
        }

        public static bool Eq1_OLD(ST<FUNC, TERM, SORT> A, ST<FUNC, TERM, SORT> B)
        {
            if (!object.Equals(A.solver, B.solver))
                throw new AutomataException(AutomataExceptionKind.SolversAreNotIdentical);

            if (!object.Equals(A.inputSort, B.inputSort) || !object.Equals(A.outputSort, B.outputSort))
                throw new AutomataException(AutomataExceptionKind.SortMismatch);

            if (A.automaton.IsEmpty || B.automaton.IsEmpty)
                return true;

            var solver = A.solver;
            var ftb = A.solver;
            //the register sort of all rules in AxB is MkTupleSort(A.RegisterSort,B.RegisterSort)
            var AxB = Automaton<Rule<TERM>>.MkProduct_(A.automaton, B.automaton, new STProductSolver(solver, A.registerSort, B.registerSort));
            if (AxB.IsEmpty) //there are no inputs in common
                return true;

            AxB.isEpsilonFree = true;

            var stAxB = new ST<FUNC, TERM, SORT>(solver, string.Format("{0}x{1}", A.name, B.name),
                solver.MkTuple(A.initReg, B.initReg), A.inputSort, A.outputSort, solver.MkTupleSort(A.registerSort, B.registerSort),
                AxB, false);

            var stAxB_lifted = stAxB.LiftIputSortToList();
            //stAxB_lifted.EliminateIntermediateStates();
            //var stAxB_simplified = stAxB_lifted.DeleteRegister();

            //if (stAxB_simplified != null)
            //{
            //    stAxB_simplified.ShowGraph(1);
            //    var testSFA = stAxB_simplified.MkCartesianExpansion().Determinize().Minimize();
            //    testSFA.ShowGraph(10);
            //}

            //create the SFT of the product, OBS!:this might not terminate
            var AB = stAxB_lifted.ExploreFull();// (stAxB_simplified != null ? stAxB_simplified : stAxB_lifted.ExploreFull());
            if (AB.automaton.IsEmpty)
                return true;

            var z3p = solver;
            var _i = ftb.MkVar(0, AB.InputSort);

            var Q = new Dictionary<int, Tuple<Seq, Seq>>(); //promises

            Stack<int> stack = new Stack<int>();

            Q[AB.InitialState] = new Tuple<Seq, Seq>(Seq.Empty, Seq.Empty); //the initial promise is the pair of empty sequences

            stack.Push(AB.InitialState);
            HashSet<int> visited = new HashSet<int>();
            visited.Add(AB.InitialState);

            #region helper methods
            Func<int, Seq, Seq, bool> AddToSearch = (p, a, b) =>
            {
                if (visited.Add(p))
                {
                    stack.Push(p);
                    Q[p] = new Tuple<Seq, Seq>(a, b);
                    return true;
                }
                return false;
            };
            #endregion

            //search for invalid common outputs
            while (stack.Count > 0)
            {
                var curr = stack.Pop();
                foreach (var move in AB.automaton.GetMovesFrom(curr))
                {
                    var a = Q[curr].Item1;  //the promise from A
                    var b = Q[curr].Item2; //the promise from B
                    if (move.Label.IsFinal)
                    {
                        if (!a.Concat(move.Label.Yields).Equals(b.Concat(move.Label.Yields2)))
                            return false; //there is a conflict in final outputs
                    }
                    else
                    {
                        TERM[] y1 = new TERM[move.Label.Yields.Length + a.Length];
                        TERM[] y2 = new TERM[move.Label.Yields2.Length + b.Length];
                        for (int i = 0; i < y1.Length; i++)
                            y1[i] = (i < a.Length ? a[i] : move.Label.Yields[i - a.Length]);
                        for (int i = 0; i < y2.Length; i++)
                            y2[i] = (i < b.Length ? b[i] : move.Label.Yields2[i - b.Length]);

                        int m = Math.Min(y1.Length, y2.Length);

                        #region check that the outputs coincide upto m for all inputs
                        TERM[] yieldDisequalities = new TERM[m];
                        for (int i = 0; i < m; i++)
                        {
                            yieldDisequalities[i] = z3p.MkNeq(y1[i], y2[i]);
                        }
                        TERM assert = z3p.MkAnd(move.Label.Guard, z3p.MkOr(yieldDisequalities));
                        if (z3p.IsSatisfiable(assert))
                        {
                            return false; //NOT EQUIVALENT, input c can yield different outputs
                        }
                        #endregion

                        //check the rests
                        TERM[] y1rest = new TERM[y1.Length - m];
                        TERM[] y2rest = new TERM[y2.Length - m];
                        for (int i = 0; i < y1rest.Length; i++)
                            y1rest[i] = y1[m + i];
                        for (int i = 0; i < y2rest.Length; i++)
                            y2rest[i] = y2[m + i];

                        if (y1rest.Length > 0) //y2rest.Length == 0
                        {
                            //check if there exist two inputs that can result in different outputs 
                            //TERM i1;
                            //TERM i2;
                            if (CheckIfTwoDistinguishingInputsExist(ftb, move.Label.Guard, y1rest, _i /*, out i1, out i2 */))
                            {
                                return false; // distinct pending outputs exist
                            }
                            else //we know that at least one input is possible
                            {
                                var i1 = GetOneMember(_i, ftb, move);
                                var s = new Seq(Array.ConvertAll(y1rest, y => ftb.Simplify(z3p.ApplySubstitution(y, _i, i1))));
                                bool added = AddToSearch(move.TargetState, s, Seq.Empty);
                                if (!added && !Q[move.TargetState].Equals(new Tuple<Seq, Seq>(s, Seq.Empty)))
                                    return false; // distinct pending outputs exist
                            }
                        }
                        else //y1rest.Length == 0 and y2rest.Length > 0
                        {
                            //check if there exist two input chars that can result in different outputs 
                            //TERM i1;
                            //TERM i2;
                            if (CheckIfTwoDistinguishingInputsExist(ftb, move.Label.Guard, y2rest, _i /*, out i1, out i2*/))
                            {
                                return false; // distinct pending outputs
                            }
                            else //we know that at least one input is possible
                            {

                                var i1 = GetOneMember(_i, ftb, move);
                                var s = new Seq(Array.ConvertAll(y1rest, y => ftb.Simplify(z3p.ApplySubstitution(y, _i, i1))));
                                bool added = AddToSearch(move.TargetState, Seq.Empty, s);
                                if (!added && !Q[move.TargetState].Equals(new Tuple<Seq, Seq>(Seq.Empty, s)))
                                    return false; // distinct pending outputs
                            }
                        }
                    }
                }
            }
            return true;
        }


        bool isMultiInput = false;

        /// <summary>
        /// Returns true iff the ST uses multiple input characters rules. In this case the input character is a list sort.
        /// </summary>
        public bool IsMultiInput
        {
            get { return isMultiInput; }
        }

        /// <summary>
        /// Applies the concrete input to produce the concrete output.
        /// </summary>
        /// <param name="input">concrete input</param>
        public IEnumerable<TERM> Apply(IEnumerable<TERM> input)
        {
            var q = InitialState;
            var r = initReg;
            foreach (var i in input)
            {
                var m = ChooseMove(i, q, r);
                foreach (var f in m.Label.Yields)
                    yield return solver.Simplify(solver.ApplySubstitution(f, InputVar, i, RegisterVar, r));
                q = m.TargetState;
                r = solver.Simplify(solver.ApplySubstitution(m.Label.Update, InputVar, i, RegisterVar, r));
            }
            var fr = ChooseFinalRule(q, r);
            foreach (var y in fr.Yields)
                yield return solver.Simplify(solver.ApplySubstitution(y, RegisterVar, r));
        }

        /// <summary>
        /// Applies the concrete input to produce the secondary concrete output in case of a product ST.
        /// </summary>
        /// <param name="input">concrete input</param>
        public IEnumerable<TERM> Apply2(IEnumerable<TERM> input)
        {
            var q = InitialState;
            var r = initReg;
            foreach (var i in input)
            {
                var m = ChooseMove(i, q, r);
                foreach (var f in m.Label.Yields2)
                    yield return solver.Simplify(solver.ApplySubstitution(f, InputVar, i, RegisterVar, r));
                q = m.TargetState;
                r = solver.Simplify(solver.ApplySubstitution(m.Label.Update, InputVar, i, RegisterVar, r));
            }
            var fr = ChooseFinalRule(q, r);
            foreach (var y in fr.Yields2)
                yield return solver.Simplify(solver.ApplySubstitution(y, RegisterVar, r));

        }

        Move<Rule<TERM>> ChooseMove(TERM i, int q, TERM r)
        {
            foreach (var m in GetNonFinalMovesFrom(q))
            {
                TERM test = solver.Simplify(solver.ApplySubstitution(m.Label.Guard, InputVar, i, RegisterVar, r));
                if (test.Equals(solver.True))
                    return m;
            }
            throw new AutomataException(AutomataExceptionKind.AutomatonInvalidInput);
        }

        Rule<TERM> ChooseFinalRule(int q, TERM r)
        {
            foreach (var f in GetFinalRules(q))
            {
                TERM test = solver.Simplify(solver.ApplySubstitution(f.Guard, RegisterVar, r));
                if (test.Equals(solver.True))
                    return f;
            }
            throw new AutomataException(AutomataExceptionKind.AutomatonInvalidInput);
        }

        /// <summary>
        /// Given a product ST, lift the input character sort s to the sort List(s). 
        /// </summary>
        ST<FUNC, TERM, SORT> LiftIputSortToList()
        {
            SORT liftedInputSort = solver.MkListSort(this.inputSort);
            TERM liftedInputVar = solver.MkVar(0, liftedInputSort);
            TERM firstOfLiftedInputVar = solver.MkFirstOfList(liftedInputVar);

            List<Move<Rule<TERM>>> liftedMoves = new List<Move<Rule<TERM>>>();
            foreach (var move in GetMoves())
            {
                if (move.Label.IsFinal)
                {
                    liftedMoves.Add(move);
                    continue;
                }

                TERM cond = solver.MkAnd(solver.ApplySubstitution(move.Label.Guard, InputVar, firstOfLiftedInputVar), 
                                         solver.MkIsCons(liftedInputVar));
                TERM upd = solver.ApplySubstitution(move.Label.Update, InputVar, firstOfLiftedInputVar);

                TERM[] yields = Array.ConvertAll(move.Label.Yields, y => solver.ApplySubstitution(y, InputVar, firstOfLiftedInputVar));

                TERM[] yields2 = (move.Label.Yields2 == null ? null : Array.ConvertAll(move.Label.Yields2, y => solver.ApplySubstitution(y, InputVar, firstOfLiftedInputVar)));

                Rule<TERM> r = new Rule<TERM>(1, cond, yields, yields2, upd);

                liftedMoves.Add(Move<Rule<TERM>>.Create(move.SourceState, move.TargetState, r));
            }

            var aut = Automaton<Rule<TERM>>.Create(null, automaton.InitialState, automaton.GetFinalStates(), liftedMoves);

            var st = new ST<FUNC, TERM, SORT>(solver, "lift_" + name, initReg, liftedInputSort, outputSort, registerSort, aut, false);
            st.isMultiInput = true;
            return st;
        }

        /// <summary>
        /// Eliminate intermediate states. Assumes lifted input and possibly product ST.
        /// </summary>
        void EliminateIntermediateStates()
        {
            ConsList<PrioritizedState> statesToEliminate = null;

            Func<int, int> Priority = st =>
                {
                    int indegree = 0;
                    int outdegree = 0;
                    foreach (var mv in automaton.GetMovesFrom(st))
                        if (!mv.Label.IsFinal)
                            outdegree += 1;
                    foreach (var mv in automaton.GetMovesTo(st))
                        if (!mv.Label.IsFinal)
                            indegree += 1;
                    return indegree * outdegree;
                };
    
            //make a new final state 
            //all final moves are treated as moves to this final state.
            int new_final_state = automaton.MkNewFinalState();
            var finalrule = new Rule<TERM>(0, solver.True, new TERM[] { }, new TERM[] { }, default(TERM));
            automaton.AddMove(Move<Rule<TERM>>.Create(new_final_state, new_final_state, finalrule));

            Predicate<int> CanBeEliminated = st =>
            {
                if (st == InitialState || st == new_final_state)
                    return false;
                foreach (var mv in GetNonFinalMovesFrom(st))
                    if (mv.TargetState == st)
                        return false;
                return true;
            };

            foreach (int state in GetStates())
            {
                if (CanBeEliminated(state)) 
                {
                    var st = new PrioritizedState(state, Priority(state));
                    if (statesToEliminate == null)
                        statesToEliminate = new ConsList<PrioritizedState>(st);
                    else if (st < statesToEliminate.First)
                        statesToEliminate = new ConsList<PrioritizedState>(st, statesToEliminate);
                    else
                    {
                        var curr = statesToEliminate;
                        while (curr.Rest != null && curr.Rest.First < st)
                            curr = curr.Rest;
                        curr.Rest = new ConsList<PrioritizedState>(st, curr.Rest);
                    }
                }
            }
            if (statesToEliminate == null)
                return; //nothing to eliminate

            //Note: a state may become a loop during the elimination
            HashSet<int> newLoopStates = new HashSet<int>();

            foreach (var pstate in statesToEliminate)
            {
                int state = pstate.id;
                if (newLoopStates.Contains(state))
                    continue;

                List<Move<Rule<TERM>>> new_moves = new List<Move<Rule<TERM>>>();

                #region construct new_moves
                foreach (var move_in in automaton.GetMovesTo(state))
                {
                    if (!move_in.Label.IsFinal)
                    {
                        var kthRestOfInput = KthRest(move_in.Label.k, InputVar);

                        var guard_in = move_in.Label.Guard;
                        var register_in = move_in.Label.Update;
                        var y1_in = move_in.Label.Yields;
                        var y2_in = (move_in.Label.Yields2 == null ? new TERM[] { } : move_in.Label.Yields2);

                        //move_out may be final
                        foreach (var move_out in automaton.GetMovesFrom(state))
                        {
                            var guard_out = move_out.Label.Guard;
                            var register_out = move_out.Label.Update;
                            var y1_out = move_out.Label.Yields;
                            var y2_out = (move_out.Label.Yields2 == null ? new TERM[] { } : move_out.Label.Yields2);


                            //-------- core step -----------
                            //combine into a single move over composed characters

                            var guard2 = solver.ApplySubstitution(guard_out, InputVar, kthRestOfInput, RegisterVar, register_in);
                            var guard = solver.MkAnd(guard_in, guard2);

                            if (solver.IsSatisfiable(guard))
                            {
                                //just use the initial register value when going to the new final state
                                var register = (move_out.Label.IsFinal ? InitialRegister : solver.Simplify(solver.ApplySubstitution(register_out, InputVar, kthRestOfInput, RegisterVar, register_in)));
                                var y1_2 = Array.ConvertAll(y1_out, y => solver.Simplify(solver.ApplySubstitution(y, InputVar, kthRestOfInput, RegisterVar, register_in)));
                                var y2_2 = Array.ConvertAll(y2_out, y => solver.Simplify(solver.ApplySubstitution(y, InputVar, kthRestOfInput, RegisterVar, register_in)));
                                var y1 = new Sequence<TERM>(y1_in).Append(y1_2).ToArray();
                                var y2 = new Sequence<TERM>(y2_in).Append(y2_2).ToArray();

                                guard = solver.Simplify(guard);

                                int k = move_in.Label.k + move_out.Label.k;
                                var rule = new Rule<TERM>(k, guard, y1, y2, register);

                                var move = (move_out.Label.IsFinal ? 
                                    Move<Rule<TERM>>.Create(move_in.SourceState, new_final_state, rule) :
                                    Move<Rule<TERM>>.Create(move_in.SourceState, move_out.TargetState, rule));

                                if (move.SourceState == move.TargetState)
                                    newLoopStates.Add(move.SourceState);

                                new_moves.Add(move);
                            }

                        }
                    }
                }
                #endregion

                automaton.RemoveTheState(state);
                foreach (var move in new_moves)
                    automaton.AddMove(move);
            }
        }


        /// <summary>
        /// Returns the equivalent SFT by deleting the register if all moves are register-independent. Returns null otherwise.
        /// </summary>
        public ST<FUNC, TERM, SORT> DeleteRegister()
        {
            List<Move<Rule<TERM>>> newMoves = new List<Move<Rule<TERM>>>();
            foreach (var move in GetMoves())
            {
                Move<Rule<TERM>> newMove;
                if (IsRegisterIndependent(move, out newMove))
                    newMoves.Add(newMove);
                else
                    return null;
            }
            var aut = Automaton<Rule<TERM>>.Create(null, automaton.InitialState, automaton.GetFinalStates(), newMoves);
            var sft = new ST<FUNC, TERM, SORT>(solver, "del_reg_" + name, solver.UnitConst, inputSort, outputSort, solver.UnitSort, aut, noYields);
            sft.isMultiInput = isMultiInput;
            return sft;
        }

        private bool IsRegisterIndependent(Move<Rule<TERM>> move, out Move<Rule<TERM>> fstMove)
        {
            var rule = move.Label;
            TERM guard = rule.guard;
            TERM[] yields1 = rule.Yields;
            TERM[] yields2 = rule.Yields2;
            if (ContainsRegisterVar(guard))
            {
                guard = solver.ApplySubstitution(guard, RegisterVar, initReg);
                var pred = solver.MkNeq(guard, rule.Guard);
                if (solver.IsSatisfiable(pred))
                {
                    fstMove = null;
                    return false;
                }
            }
            if (ContainsRegisterVar(yields1))
            {
                yields1 = Array.ConvertAll(rule.Yields, y => solver.ApplySubstitution(y, RegisterVar, initReg));
                TERM[] cases = new TERM[yields1.Length];
                for (int i = 0; i < cases.Length; i++)
                    cases[i] = solver.MkNeq(rule.Yields[i], yields1[i]);
                var pred = solver.MkAnd(guard, solver.MkOr(cases));
                if (solver.IsSatisfiable(pred))
                {
                    fstMove = null;
                    return false;
                }
            }
            if (yields2 != null && ContainsRegisterVar(yields2))
            {
                yields2 = Array.ConvertAll(rule.Yields2, y => solver.ApplySubstitution(y, RegisterVar, initReg));
                TERM[] cases = new TERM[yields2.Length];
                for (int i = 0; i < cases.Length; i++)
                    cases[i] = solver.MkNeq(rule.Yields2[i], yields2[i]);
                var pred = solver.MkAnd(guard, solver.MkOr(cases));
                if (solver.IsSatisfiable(pred))
                {
                    fstMove = null;
                    return false;
                }
            }
            fstMove = Move<Rule<TERM>>.Create(move.SourceState, move.TargetState, new Rule<TERM>(rule.k, guard, yields1, yields2, solver.UnitConst));
            return true;
        }

        bool ContainsRegisterVar(params TERM[] ts)
        {
            foreach (var t in ts)
                foreach (var v in solver.GetVars(t))
                    if (v.Equals(this.RegisterVar))
                        return true;
            return false;
        }

        TERM KthRest(int k, TERM list)
        {
            TERM res = list;
            while (k > 0)
            {
                res = solver.MkRestOfList(res);
                k = k - 1;
            }
            return res;
        }

        //bool TryGetSimpleEliminationState(out int state)
        //{
        //    foreach (int s in automaton.States)
        //    {
        //        if (automaton.InitialState != s && !automaton.IsFinalState(s) && !automaton.IsLoopState(s) &&
        //            automaton.OutDegree(s) == 1 && automaton.InDegree(s) == 1)
        //        {
        //            state = s;
        //            return true;
        //        }
        //    }
        //    state = -1;
        //    return false;
        //}

        private static TERM GetOneMember(TERM inputVar, IContext<FUNC, TERM, SORT> ftb, Move<Rule<TERM>> move)
        {
            return ftb.MainSolver.FindOneMember(ftb.MkAnd(ftb.MkEq(inputVar, inputVar), move.Label.Guard)).Value;
        }

        private static bool CheckIfTwoDistinguishingInputsExist(IContext<FUNC, TERM, SORT> solver, TERM guard, TERM[] ys, TERM _i /*, out TERM i1, out TERM i2*/)
        {
            solver.MainSolver.Push();
            TERM _i1 = solver.MkFreshConst("_i1", solver.GetSort(_i));
            TERM _i2 = solver.MkFreshConst("_i2", solver.GetSort(_i));
            TERM guard1 = solver.ApplySubstitution(guard, _i, _i1);
            TERM guard2 = solver.ApplySubstitution(guard, _i, _i2);
            TERM[] ys1 = Array.ConvertAll(ys, y => solver.ApplySubstitution(y, _i, _i1));
            TERM[] ys2 = Array.ConvertAll(ys, y => solver.ApplySubstitution(y, _i, _i2));
            TERM[] ys1_neq_ys2_cases = new TERM[ys.Length];
            for (int i = 0; i < ys.Length; i++)
                ys1_neq_ys2_cases[i] = solver.MkNeq(ys1[i], ys2[i]);
            TERM ys1_neq_ys2 = solver.MkOr(ys1_neq_ys2_cases);
            TERM assert = solver.MkAnd(guard1, solver.MkAnd( guard2, ys1_neq_ys2));
            var model = solver.MainSolver.GetModel(assert /*, _i1, _i2*/);
            solver.MainSolver.Pop();
            if (model == null)
            {
                //i1 = default(TERM);
                //i2 = default(TERM);
                return false;
            }
            else
            {
                //i1 = model[_i1].Value;
                //i2 = model[_i2].Value;
                return true;
            }
        }

        private static Tuple<IValue<TERM>,IValue<TERM>> GetDistinguishingInputs(IContext<FUNC, TERM, SORT> solver, TERM guard, TERM[] ys, TERM _i)
        {
            solver.MainSolver.Push();
            TERM _i1 = solver.MkFreshConst("_i1", solver.GetSort(_i));
            TERM _i2 = solver.MkFreshConst("_i2", solver.GetSort(_i));
            TERM guard1 = solver.ApplySubstitution(guard, _i, _i1);
            TERM guard2 = solver.ApplySubstitution(guard, _i, _i2);
            TERM[] ys1 = Array.ConvertAll(ys, y => solver.ApplySubstitution(y, _i, _i1));
            TERM[] ys2 = Array.ConvertAll(ys, y => solver.ApplySubstitution(y, _i, _i2));
            TERM[] ys1_neq_ys2_cases = new TERM[ys.Length];
            for (int i = 0; i < ys.Length; i++)
                ys1_neq_ys2_cases[i] = solver.MkNeq(ys1[i], ys2[i]);
            TERM ys1_neq_ys2 = solver.MkOr(ys1_neq_ys2_cases);
            TERM assert = solver.MkAnd(guard1, solver.MkAnd(guard2, ys1_neq_ys2));
            var model = solver.MainSolver.GetModel(assert, _i1, _i2);
            solver.MainSolver.Pop();
            if (model == null)
            {
                return null;
            }
            else
            {
                var i1 = model[_i1];
                var i2 = model[_i2];
                var res = Tuple.Create(i1,i2);
                return res;
            }
        }

        /// <summary>
        /// Returns false iff A and B are not 1-equal, i.e., 
        /// if there exists u,v,w s.t. A(u,v) and B(u,w) but v!=w.
        /// Note that Eq1(A,A)=false iff A is not single-valued.
        /// </summary>
        public static bool Eq1_v2(ST<FUNC, TERM, SORT> A, ST<FUNC, TERM, SORT> B) 
        {
            if (!object.Equals(A.solver, B.solver))
                throw new AutomataException(AutomataExceptionKind.SolversAreNotIdentical);

            if (!object.Equals(A.inputSort, B.inputSort) || !object.Equals(A.outputSort, B.outputSort))
                throw new AutomataException(AutomataExceptionKind.SortMismatch);

            if (A.automaton.IsEmpty || B.automaton.IsEmpty)
                return true;

            var solver = A.solver;
            var ftb = A.solver;
            SORT inputSort = A.inputSort;
            SORT outputSort = A.outputSort;
            //the register sort of all rules in AxB is MkTupleSort(A.RegisterSort,B.RegisterSort)
            var AxB = Automaton<Rule<TERM>>.MkProduct_(A.automaton, B.automaton, new STProductSolver(solver, A.registerSort, B.registerSort));
            if (AxB.IsEmpty) //there are no inputs in common
                return true;

            var stAxB = new ST<FUNC, TERM, SORT>(solver, string.Format("{0}x{1}", A.name, B.name),
                solver.MkTuple(A.initReg, B.initReg), inputSort, outputSort, solver.MkTupleSort(A.registerSort, B.registerSort),
                AxB, false);

            //create the SFT of the product, OBS!:this might not terminate
            var AB = stAxB.ExploreFull();
            if (AB.automaton.IsEmpty)
                return true;

            var z3p = solver;
            var _i = ftb.MkVar(0, inputSort);

            var Q = new Dictionary<int, Tuple<Seq, Seq>>(); //promises

            Stack<int> stack = new Stack<int>();

            Q[AB.InitialState] = new Tuple<Seq, Seq>(Seq.Empty, Seq.Empty); //the initial promise is the pair of empty sequences

            stack.Push(AB.InitialState);
            HashSet<int> visited = new HashSet<int>();
            visited.Add(AB.InitialState);

            #region helper methods
            Func<int, Seq, Seq, bool> AddToSearch = (p, a, b) =>
            {
                if (visited.Add(p))
                {
                    stack.Push(p);
                    Q[p] = new Tuple<Seq, Seq>(a, b);
                    return true;
                }
                return false;
            };
            #endregion

            //search for invalid common outputs
            while (stack.Count > 0)
            {
                var curr = stack.Pop();
                foreach (var move in AB.automaton.GetMovesFrom(curr))
                {
                    var a = Q[curr].Item1;  //the promise from A
                    var b = Q[curr].Item2; //the promise from B
                    if (move.Label.IsFinal)
                    {
                        if (!a.Concat(move.Label.Yields).Equals(b.Concat(move.Label.Yields2)))
                            return false; //there is a conflict in final outputs
                    }
                    else
                    {
                        TERM[] y1 = new TERM[move.Label.Yields.Length + a.Length];
                        TERM[] y2 = new TERM[move.Label.Yields2.Length + b.Length];
                        for (int i = 0; i < y1.Length; i++)
                            y1[i] = (i < a.Length ? a[i] : move.Label.Yields[i - a.Length]);
                        for (int i = 0; i < y2.Length; i++)
                            y2[i] = (i < b.Length ? b[i] : move.Label.Yields2[i - b.Length]);

                        int m = Math.Min(y1.Length, y2.Length);

                        #region check that the outputs coincide upto m for all inputs
                        TERM[] yieldDisequalities = new TERM[m];
                        for (int i = 0; i < m; i++)
                        {
                            yieldDisequalities[i] = z3p.MkNeq(y1[i], y2[i]);
                        }
                        TERM assert = z3p.MkAnd(move.Label.Guard, z3p.MkOr(yieldDisequalities));
                        if (z3p.IsSatisfiable(assert))
                        {
                            return false; //NOT EQUIVALENT, input c can yield different outputs
                        }
                        #endregion

                        //check the rests
                        TERM[] y1rest = new TERM[y1.Length - m];
                        TERM[] y2rest = new TERM[y2.Length - m];
                        for (int i = 0; i < y1rest.Length; i++)
                            y1rest[i] = y1[m + i];
                        for (int i = 0; i < y2rest.Length; i++)
                            y2rest[i] = y2[m + i];

                        if (y1rest.Length > 0) //y2rest.Length == 0
                        {
                            //check if there exist two inputs that can result in different outputs 
                            //TERM i1;
                            //TERM i2;
                            if (CheckIfTwoDistinguishingInputsExist(ftb, move.Label.Guard, y1rest, _i /*, out i1, out i2 */))
                            {
                                return false; // distinct pending outputs exist
                            }
                            else //we know that at least one input is possible
                            {
                                var i1 =  GetOneMember(_i, ftb, move);
                                var s = new Seq(Array.ConvertAll(y1rest, y => ftb.Simplify(z3p.ApplySubstitution(y, _i, i1))));
                                bool added = AddToSearch(move.TargetState, s, Seq.Empty);
                                if (!added && !Q[move.TargetState].Equals(new Tuple<Seq, Seq>(s, Seq.Empty)))
                                    return false; // distinct pending outputs exist
                            }
                        }
                        else //y1rest.Length == 0 and y2rest.Length > 0
                        {
                            //check if there exist two input chars that can result in different outputs 
                            //TERM i1;
                            //TERM i2;
                            if (CheckIfTwoDistinguishingInputsExist(ftb, move.Label.Guard, y2rest, _i /*, out i1, out i2*/))
                            {
                                return false; // distinct pending outputs
                            }
                            else //we know that at least one input is possible
                            {
                                var i1 = GetOneMember(_i, ftb, move);
                                var s = new Seq(Array.ConvertAll(y1rest, y => ftb.Simplify(z3p.ApplySubstitution(y, _i, i1))));
                                bool added = AddToSearch(move.TargetState, Seq.Empty, s);
                                if (!added && !Q[move.TargetState].Equals(new Tuple<Seq, Seq>(Seq.Empty, s)))
                                    return false; // distinct pending outputs
                            }
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Enumerates incrementally all moves from the given start state and concrete start register value.
        /// Yields and guards of all rules are concretized with the given start register value.
        /// The update of a nonfinal rule is a resulting concrete register value
        /// and the guard restricts the input with respect to that resulting register value.
        /// </summary>
        /// <param name="state">given start state</param>
        /// <param name="reg">given concrete start register value</param>
        /// <returns>all possible moves with concrete register instantiations</returns>
        public IEnumerable<Move<Rule<TERM>>> ExploreFrom(int state, TERM reg)
        {
            foreach (var move in automaton.GetMovesFrom(state))
            {
                if (move.Label.IsFinal)
                {
                    if (solver.IsSatisfiable(solver.ApplySubstitution(move.Label.Guard, RegisterVar, reg)))
                    {
                        var moveInst = new Move<Rule<TERM>>(move.SourceState, move.TargetState,
                            new Rule<TERM>(0, Solver.True,
                                Array.ConvertAll(move.Label.Yields, y => solver.ApplySubstitution(y, RegisterVar, reg)),
                                (move.Label.Yields2 == null ? null :
                                Array.ConvertAll(move.Label.Yields2, y => solver.ApplySubstitution(y, RegisterVar, reg))),
                                default(TERM)));
                        yield return moveInst;
                    }

                }
                else
                {
                    TERM g = solver.ApplySubstitution(move.Label.Guard, RegisterVar, reg);
                    if (solver.IsSatisfiable(g))
                    {
                        var upd = solver.ApplySubstitution(move.Label.Update, RegisterVar, reg);
                        foreach (var updVal in FindDistinctRegisterValues(g, upd))
                        {
                            var moveInst = new Move<Rule<TERM>>(move.SourceState, move.TargetState,
                                new Rule<TERM>(move.Label.k, solver.MkAnd(g, solver.MkEq(updVal, upd)),
                                    Array.ConvertAll(move.Label.Yields, y => solver.ApplySubstitution(y, RegisterVar, reg)),
                                    (move.Label.Yields2 == null ? null :
                                     Array.ConvertAll(move.Label.Yields2, y => solver.ApplySubstitution(y, RegisterVar, reg))),
                                    updVal));
                            yield return moveInst;
                        }
                    }
                }
            }

        }
        #endregion

        #region ST product solver

        private class STProductSolver : IBooleanAlgebraPositive<Rule<TERM>>
        {
            IContext<FUNC,TERM,SORT> solver;
            TERM regVar1;
            TERM regVar2;
            TERM y1;
            TERM y2;
            TERM[] eps;
            public STProductSolver(IContext<FUNC, TERM, SORT> solver, SORT regSort1, SORT regSort2)
            {
                this.solver = solver;
                this.regVar1 = solver.MkVar(1, regSort1);
                this.regVar2 = solver.MkVar(1, regSort2);
                var regVar = solver.MkVar(1, solver.MkTupleSort(regSort1, regSort2));
                this.y1 = solver.MkProj(0, regVar);
                this.y2 = solver.MkProj(1, regVar);
                this.eps = new TERM[0] { };
            }


            #region IPosBoolAlg<Rule<T>> Members

            #region disjunction is not used because epsilon transitions are not allowed
            public Rule<TERM> MkOr(Rule<TERM> l1, Rule<TERM> l2)
            {
                //cannot occur, unless there is a bug
                throw new AutomataException(AutomataExceptionKind.InternalError);
            }
            #endregion

            public Rule<TERM> MkAnd(Rule<TERM> r1, Rule<TERM> r2)
            {
                TERM g1 = solver.ApplySubstitution(r1.Guard, regVar1, y1); 
                TERM g2 = solver.ApplySubstitution(r2.Guard, regVar2, y2); 
                TERM g = solver.MkAnd(g1, g2);
                TERM[] yields1 = Array.ConvertAll(r1.Yields, outp => solver.ApplySubstitution(outp, regVar1, y1));
                TERM[] yields2 = Array.ConvertAll(r2.Yields, outp => solver.ApplySubstitution(outp, regVar2, y2));

                if (r1.IsFinal && r2.IsFinal)
                    return new Rule<TERM>(0, g, yields1, yields2, default(TERM));

                    //TBD: what if multi-input ?
                else if (!r1.IsFinal && !r2.IsFinal) 
                    return new Rule<TERM>(1, g, yields1, yields2,
                        solver.MkTuple(solver.ApplySubstitution(r1.Update, regVar1, y1), solver.ApplySubstitution(r2.Update, regVar2, y2)));

                else
                    return new Rule<TERM>(0, solver.False, eps, eps, default(TERM)); 

            }

            public bool IsSatisfiable(Rule<TERM> rule)
            {
                return solver.IsSatisfiable(rule.Guard);
            }

            #endregion


            public Rule<TERM> True
            {
                get { throw new NotImplementedException(); }
            }

            public Rule<TERM> False
            {
                get { throw new NotImplementedException(); }
            }
        }
        #endregion

        #region sequences of terms and pairs of sequences of terms

        private class Seq
        {
            internal TERM[] elems;

            public TERM this[int i]
            {
                get { return elems[i]; }
            }

            public int Length { get { return elems.Length; } }

            public static Seq Empty = new Seq();

            public Seq(params TERM[] elems)
            {
                this.elems = elems;
            }

            public Seq Concat(params TERM[] newElems)
            {
                if (newElems.Length == 0)
                    return this;
                else
                {
                    TERM[] elems1 = new TERM[elems.Length + newElems.Length];
                    Array.Copy(elems, elems1, elems.Length);
                    Array.Copy(newElems, 0, elems1, elems.Length, newElems.Length);
                    return new Seq(elems1);
                }
            }

            public override bool Equals(object obj)
            {
                Seq s = obj as Seq;
                if (s == null || s.elems.Length != elems.Length)
                    return false;
                for (int i = 0; i < elems.Length; i++)
                    if (!object.Equals(elems[i], s.elems[i]))
                        return false;
                return true;
            }

            public override int GetHashCode()
            {
                int res = 0;
                for (int i = 0; i < elems.Length; i++)
                    res += (elems[i].GetHashCode() << i);
                return res;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                for (int i = 0; i < elems.Length; i++)
                {
                    if (i > 0)
                        sb.Append(",");
                    sb.Append(elems[i].ToString());
                }
                sb.Append("]");
                return sb.ToString();
            }
        }

        //private class SS
        //{
        //    Seq a;
        //    Seq b;
        //    public SS(Seq a, Seq b)
        //    {
        //        this.a = a;
        //        this.b = b;
        //    }

        //    public override bool Equals(object obj)
        //    {
        //        SS s = obj as SS;
        //        if (s == null)
        //            return false;

        //        return (a.Equals(s.a) && b.Equals(s.b));
        //    }

        //    public override int GetHashCode()
        //    {
        //        return a.GetHashCode() + (2*b.GetHashCode());
        //    }
        //}

        #endregion

        bool alreadySimplified = false;
        /// <summary>
        /// Replaces terms by logically equivalent simpler terms in the ST.
        /// </summary>
        public void Simplify()
        {
            if (alreadySimplified)
                return;

            alreadySimplified = true;
            this.initReg = solver.ToNNF(solver.Simplify(initReg));
            foreach (var move in automaton.GetMoves())
            {
                move.Label.guard = solver.ToNNF(solver.Simplify(move.Label.guard));
                //if the guard only contains the input var
                //check if the guard only holds for up to two values
                var guardVars = new List<TERM>(solver.GetVars(move.Label.guard));
                if (guardVars.Count == 1)
                {
                    bool simplify = true;
                    List<TERM> solutions = new List<TERM>();
                    foreach (var solution in solver.MainSolver.FindAllMembers(move.Label.guard))
                    {
                        if (solutions.Count == 2)
                        {
                            //too many solutions, abort the simplification
                            simplify = false;
                            break;
                        }
                        else
                        {
                            solutions.Add(solution.Value);
                        }
                    }
                    if (simplify) //note that the solution list cannot be empty
                    {
                        TERM simplerGuard = solver.MkEq(guardVars[0], solutions[0]);
                        if (solutions.Count == 2)
                        {
                            simplerGuard = solver.MkOr(simplerGuard, solver.MkEq(guardVars[0], solutions[1]));
                        }
                        move.Label.guard = simplerGuard;
                    }
                }

                for (int i = 0; i < move.Label.Yields.Length; i++)
                {
                    move.Label.Yields[i] = solver.ToNNF(solver.Simplify(move.Label.Yields[i]));
                }
                if (move.Label.Yields2 != null)
                {
                    for (int i = 0; i < move.Label.Yields2.Length; i++)
                    {
                        move.Label.Yields2[i] = solver.ToNNF(solver.Simplify(move.Label.Yields2[i]));
                    }
                }
                if (!move.Label.IsFinal)
                {
                    move.Label.update = solver.ToNNF(solver.Simplify(move.Label.update));
                }
            }
        }


        /// <summary>
        /// Provides a counterexample to 1-equality between STs returned by the ST.Diff method.
        /// </summary>
        internal class CounterExample : ICounterexample<TERM>
        {
            int inputLength;
            IValue<TERM> input;
            IValue<TERM> output1;
            IValue<TERM> output2;

            public CounterExample(int inputLength, IValue<TERM> input, IValue<TERM> output1, IValue<TERM> output2)
            {
                this.input = input;
                this.inputLength = inputLength;
                this.output1 = output1;
                this.output2 = output2;
            }

            #region ICounterexample<TERM> Members

            /// <summary>
            /// Length of the input sequece of the counterexample.
            /// </summary>
            public int InputLength
            {
                get { return inputLength; }
            }

            /// <summary>
            /// Input sequence that is a counterexample to 1-equality.
            /// </summary>
            public IValue<TERM> Input
            {
                get { return input; }
            }

            /// <summary>
            /// Output sequence from the first ST for the given input.
            /// </summary>
            public IValue<TERM> Output1
            {
                get { return output1; }
            }

            /// <summary>
            /// Output sequence from the second ST for the given input.
            /// </summary>
            public IValue<TERM> Output2
            {
                get { return output2; }
            }

            #endregion
        }

        public void ToDot(string file)
        {
            //if (_STb != null)
            //    _STb.ToDot(file);
            //else
                this.SaveAsDot(file);
        }

        public void ToDot(System.IO.TextWriter file)
        {
            //if (_STb != null)
            //    _STb.ToDot(file);
            //else
                this.SaveAsDot(file);
        }

        //public ST<FUNC, TERM, SORT> CreateInstance(int k)
        //{

        //}

        public bool IsTrueForAllRegisterUpdates(Predicate<TERM> pred)
        {
            foreach (var move in automaton.GetMoves())
            {
                if (!move.Label.IsFinal)
                    if (!pred(move.Label.update))
                        return false;
            }
            return true;
        }


        public IBooleanAlgebra<Rule<TERM>> Algebra
        {
            get { throw new NotSupportedException(); }
        }
    }

    /// <summary>
    /// Provides a counterexample to 1-equality between STs returned by the ST.Diff method.
    /// </summary>
    public interface ICounterexample<TERM>
    {
        /// <summary>
        /// Length of the input sequece of the counterexample.
        /// </summary>
        int InputLength { get; }

        /// <summary>
        /// Input sequence that is a counterexample to 1-equality.
        /// </summary>
        IValue<TERM> Input { get; }

        /// <summary>
        /// Output sequence from the first ST for the given input.
        /// </summary>
        IValue<TERM> Output1 { get; }

        /// <summary>
        /// Output sequence from the second ST for the given input.
        /// </summary>
        IValue<TERM> Output2 { get; }
    }

    internal class PrioritizedState : IComparable
    {
        readonly public int id;
        readonly int priority;
        public PrioritizedState(int id, int priority)
        {
            this.priority = priority;
            this.id = id;
        }

        public static bool operator <(PrioritizedState s, PrioritizedState t)
        {
            return s.CompareTo(t) < 0;
        }

        public static bool operator >(PrioritizedState s, PrioritizedState t)
        {
            return s.CompareTo(t) > 0;
        }

        public int CompareTo(object obj)
        {
            PrioritizedState state = obj as PrioritizedState;
            if (state == null)
                throw new AutomataException(AutomataExceptionKind.InternalError);

            if (priority < state.priority)
                return -1;

            else if (priority > state.priority)
                return 1;

            else
                return id.CompareTo(state.id);
        }

        public override bool Equals(object obj)
        {
            PrioritizedState state = obj as PrioritizedState;
            if (state == null)
                return false;

            return (state.id == id && state.priority == priority);
        }

        public override int GetHashCode()
        {
            return ((id << 4) + priority);
        }

        public override string ToString()
        {
            return string.Format("({0},priority:{1})", id, priority);
        }
    }
}
